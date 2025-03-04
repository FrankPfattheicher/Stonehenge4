﻿
// Stonehenge 4 application
var app;

function stonehengeCancelRequests() {

    if(typeof(app) == "undefined") return;
    try {
        app.activeRequests.forEach(rq => {
            rq.abort();
        });
        app.activeRequests.clear();
    } catch (error) {
        //debugger;
        if (console && console.log) console.log(error);
    }
}

function stonehengeReloadOnError(error) {
    if (console && console.log) console.log(error);
    window.location.reload();
}

function stonehengeMakeRequest(method, url, data) {
    return new Promise(function (resolve, reject) {

        const xhr = new XMLHttpRequest();
        xhr.open(method, url);
        xhr.onload = function () {
            if (this.status >= 200 && this.status < 400) {
                resolve(xhr.responseText);
            } else {
                reject({
                    status: this.status,
                    statusText: xhr.statusText
                });
            }
        };
        xhr.onerror = function () {
            reject({
                status: this.status,
                statusText: xhr.statusText
            });
        };
        xhr.send(data);
    });
}

function stonehengeMakeGetRequest(url) {
    return stonehengeMakeRequest('GET', url);
}
function stonehengeMakePostRequest(url) {
    return stonehengeMakeRequest('POST', url);
}

function stonehengeUploadFile(url, file) {

    const fileName = file.name;
    const formData = new FormData();
    let reader = new FileReader();
    reader.onload = function () {
        const data = new Blob([reader.result], { type: 'application/octet-stream' });
        formData.append('uploadFile', data, fileName);
        return stonehengeMakeRequest('POST', url, formData);
    };
    reader.readAsArrayBuffer(file);

}

async function stonehengeLoadComponent(name) {

    const srcRequest = stonehengeMakeGetRequest(name + '.js');
    const templateRequest = stonehengeMakeGetRequest(name + '.html');

    let src;
    let srcText;
    let templateText;
    [templateText, srcText] = await Promise.all([templateRequest, srcRequest]);

    try {
        src = eval(srcText)();
    } catch (error) {
        debugger;
        if (console && console.log) console.log(error);
    }

    return Vue.component('stonehenge_' + name, {
            template: templateText,
            data: src.data,
            methods: src.methods
        }
    );
}
function stonehengeGetCookie(name) {
    let i = 0; //Suchposition im Cookie
    const search = name + "=";
    const maxLen = document.cookie.length;
    while (i < maxLen) {
        if (document.cookie.substring(i, i + search.length) === search) {
            let end = document.cookie.indexOf(";", i + search.length);
            if (end < 0) {
                end = maxLen;
            }
            const cook = document.cookie.substring(i + search.length, end);
            return unescape(cook);
        }
        i++;
    }
    return "";
}

function stonehengeCopyToClipboard(text) {
    const textarea = document.createElement('textarea')
    document.body.appendChild(textarea)
    textarea.value = text
    textarea.select()
    document.execCommand('copy')
    textarea.remove()
}

function stonehengeEnableRoute(route, enabledTitle) {
    // { path: '/rrrr', name: 'rrrr', title: 'rrrr', component: () => Promise.resolve(stonehengeLoadComponent('rrrr')), visible: true }
    let found = routes.filter(function (item) { return item.name === route; })[0] || null;
    if(found) {
        found.visible = enabledTitle[0];
        found.title = enabledTitle[1];
    }
}

// Router
const routes = [
    //stonehengeAppRoutes
];

const router = new VueRouter({
    routes: routes
});

// Register a global custom directive called `v-focus`
Vue.directive('focus', {
    // When the bound element inserted into the DOM...
    inserted: function (el) {
        // Focus the element
        el.focus();
    }
})

// Register a global custom directive called `v-focus`
Vue.directive('select', {
    // When the bound element inserted into the DOM...
    inserted: function (el) {
        // Focus the element
        el.focus();
        el.select();
    }
})

// Global App Commands
function AppCommand(cmdName) {
    stonehengeMakePostRequest('Command/' + cmdName);
}
// Components

//stonehengeElements

// App
app = new Vue({
    data: {
        stonehengeReloadOnError: stonehengeReloadOnError,
        stonehengeCancelRequests: stonehengeCancelRequests,
        stonehengeMakeRequest: stonehengeMakeGetRequest,
        routes: routes,
        title: 'stonehengeAppTitle',
        activeViewModelName: '',
        activeRequests: new Set()
    },
    router: router
}).$mount('#app');

// allow window access in v-on handlers
Vue.prototype.window = window;
window.UploadFile = stonehengeUploadFile;

router.push('stonehengeRootPage');

if(stonehengeAppHandleWindowResized) {

    const debounce = (func, arguments, wait, immediate) => {
        let timeout;
        return () => {
            const context = this, args = arguments;
            const later = function() {
                timeout = null;
                if (!immediate) func.apply(context, args);
            };
            const callNow = immediate && !timeout;
            clearTimeout(timeout);
            timeout = setTimeout(later, wait);
            if (callNow) func.apply(context, args);
        };
    };

    function stonehengeHandleResize() {
        //console.log(this.outerWidth, this.outerHeight);
        stonehengeMakePostRequest('Command/WindowResized?width=' + this.outerWidth + '&height=' + this.outerHeight);
    }

    window.addEventListener('resize', debounce(stonehengeHandleResize, [], 1000, false),false);

} 
