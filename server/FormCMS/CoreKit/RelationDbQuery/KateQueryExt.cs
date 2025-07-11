using FluentResults;
using FormCMS.Core.Descriptors;
using FormCMS.Utils.DataModels;
using FormCMS.Utils.EnumExt;
using Humanizer;

namespace FormCMS.CoreKit.RelationDbQuery;

public static class KateQueryExt
{
    
    public static void ApplyJoin(
        this SqlKata.Query query, 
        IEnumerable<AttributeVector> vectors, 
        PublicationStatus? publicationStatus
        )
    {
        var root = AttributeTreeNode.Parse(vectors);
        Bfs(root, "");


        void Bfs(AttributeTreeNode node, string prefix)
        {
            var nextPrefix = prefix;

            //root doesn't have attribute
            if (node.Attribute is not null)
            {
                nextPrefix = prefix == ""
                    ? node.Attribute.Field
                    : AttributeVectorConstants.Separator + node.Attribute.Field;

                var desc = node.Attribute.GetEntityLinkDesc().Value;

                _ = node.Attribute.DataType switch
                {
                    DataType.Junction => ApplyJunctionJoin(query, node.Attribute.Junction!, prefix, nextPrefix, publicationStatus),
                    DataType.Lookup or DataType.Collection => ApplyJoin(query, desc, prefix, nextPrefix, publicationStatus),
                    _ => query
                };
            }

            foreach (var sub in node.Children)
            {
                Bfs(sub, nextPrefix);
            }
        }
    }

    private static SqlKata.Query ApplyJoin(
        SqlKata.Query query, 
        EntityLinkDesc desc, 
        string prefix, 
        string nextPrefix, 
        PublicationStatus? publicationStatus)
    {
        query.LeftJoin($"{desc.TargetEntity.TableName} as {nextPrefix}",
                desc.SourceAttribute.AddTableModifier(prefix),
                desc.TargetAttribute.AddTableModifier(nextPrefix))
            .Where(desc.TargetEntity.DeletedAttribute.AddTableModifier(nextPrefix), false);
        if (publicationStatus.HasValue)
        {
            query = query.Where(desc.TargetEntity.PublicationStatusAttribute.AddTableModifier(nextPrefix), publicationStatus.Value.Camelize());
        }
        return query;
    }

    private static SqlKata.Query ApplyJunctionJoin(SqlKata.Query query, Junction junction, 
        string prefix, string nextPrefix, PublicationStatus? publicationStatus)
    {
        var crossAlias = $"{prefix}_{junction.JunctionEntity.TableName}";
        var destAlias = prefix + nextPrefix;
        query
            .LeftJoin($"{junction.JunctionEntity.TableName} as {crossAlias}",
                junction.SourceEntity.PrimaryKeyAttribute.AddTableModifier(prefix),
                junction.SourceAttribute.AddTableModifier(crossAlias))
            .LeftJoin($"{junction.TargetEntity.TableName} as {destAlias}",
                junction.TargetAttribute.AddTableModifier(crossAlias),
                junction.TargetEntity.PrimaryKeyAttribute.AddTableModifier(destAlias))
            .Where(junction.JunctionEntity.DeletedAttribute.AddTableModifier(crossAlias), false)
            .Where(junction.TargetEntity.DeletedAttribute.AddTableModifier(destAlias), false);
        if (publicationStatus.HasValue)
        {
            query = query
                .Where(junction.TargetEntity.PublicationStatusAttribute.AddTableModifier(destAlias), publicationStatus.Value.Camelize());
        }
        return query;
    }


    public static void ApplyPagination(this SqlKata.Query query, ValidPagination pagination)
    {
        query.Offset(pagination.Offset).Limit(pagination.Limit);
    }
    
    public static void ApplySorts(this SqlKata.Query query, IEnumerable<Sort> sorts)
    {
        foreach (var sort in sorts)
        {
            if (sort.Order == SortOrder.Desc)
            {
                query.OrderByDesc(sort.Field);
            }
            else
            {
                query.OrderBy(sort.Field);
            }
        }
    }
    
    public static void ApplyValidSorts(this SqlKata.Query query, IEnumerable<ValidSort> sorts)
    {
        foreach (var sort in sorts)
        {
            var vector = sort.Vector;
            if (sort.Order == SortOrder.Desc)
            {
                query.OrderByDesc(vector.Attribute.AddTableModifier(vector.TableAlias));
            }
            else
            {
                query.OrderBy(vector.Attribute.AddTableModifier(vector.TableAlias));
            }
        }
    }

    public static Result ApplyFilters(this SqlKata.Query query, IEnumerable<ValidFilter> filters)
    {
        var result = Result.Ok();
        foreach (var filter in filters)
        {
            var filedName = filter.Vector.Attribute.AddTableModifier(filter.Vector.TableAlias);
            query.Where(q =>
            {
                foreach (var c in filter.Constraints)
                {
                    var ret = filter.MatchType == MatchTypes.MatchAny
                        ? q.ApplyOrConstraint(filedName, c.Match, c.Values.GetValues())
                        : q.ApplyAndConstraint(filedName, c.Match, c.Values.GetValues());
                    if (ret.IsFailed)
                    {
                        result.WithErrors(ret.Errors);
                        break;
                    }
                }

                return q;
            });
        }

        return result;
    }

   
    
    public static void ApplySpanFilter<T>(this SqlKata.Query? query,  ValidSpan? span,T[] sorts, Func<T, string> tableModify, Func<T, string> fullPath)
    where T:Sort
    {
        
        if (query is null || span?.EdgeItem is null)
        {
            return;
        }

        query.Where(q =>
        {
            for (var i = 0; i < sorts.Length; i++)
            {
                ApplyFilter(q, i);
                if (i < sorts.Length - 1)
                {
                    q.Or();
                }
            }
            return q;
        });
        return ;

        void ApplyFilter(SqlKata.Query q,int idx)
        {
            for (var i = 0; i < idx; i++)
            {
                ApplyEq(q, sorts[i]);
            }
            ApplyCompare(q,sorts[idx]);
        }

        void ApplyEq(SqlKata.Query q, T sort)
        {
            q.Where(tableModify(sort),  span.Edge(fullPath(sort)));
        }

        void ApplyCompare(SqlKata.Query q, T sort)
        {
            q.Where(
                tableModify(sort),
                span.Span.GetCompareOperator(sort.Order), 
                span.Edge(fullPath(sort)));
        }
        
    }
}