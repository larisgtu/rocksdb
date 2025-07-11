/*
data-gjs-type: for grapes.js attach trait
data-component-type: for back end to prepare data
* */

import {listA} from "./blocks/list-a.js";
import {cardA} from "./blocks/card-a.js";
import {contentB} from "./blocks/content-b.js";
import {ecommerceA} from "./blocks/ecommerce-a.js";
import {headerB} from "./blocks/header-b.js";
import {heroB} from "./blocks/hero-b.js";
import {threeLayerMenu} from "./blocks/three-layer-menu.js";
import {breadcrumbs} from "./blocks/breadcrumbs.js";
import {carousel} from "./blocks/carousel.js";
import {activityBar}from "./blocks/activity-bar.js"
import {topList} from "./blocks/top-list.js";
import {featuredList} from "./blocks/featured-list.js";
import {comments} from "./blocks/comments.js";
import {video} from "./blocks/video.js";
import {notificationBell} from "./blocks/notification-bell.js";
import {userAvatar} from "./blocks/user-avatar.js";

export function addCustomBlocks(editor){
    for (const {name, label, media, content, category} of customBlocks){
        editor.Blocks.add(name, {
            media: media,
            label:  label,
            content: content,
            category: category,
        });
    }
}
export const customBlocks = [
    cardA,
    contentB,
    ecommerceA,
    headerB,
    heroB,
    listA,
    threeLayerMenu,
    breadcrumbs,
    carousel,
    activityBar,
    featuredList,
    topList,
    comments,
    video,
    notificationBell,
    userAvatar
]
