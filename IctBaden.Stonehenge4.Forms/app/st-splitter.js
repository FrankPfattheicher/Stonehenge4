
mounted: function() {

    const splitter = this.$el;
    const siblings = splitter.parentElement.children;
    if(siblings.length !== 3) return;

    splitter.onmousedown = function(e) {

        const first = siblings[0];
        const second = siblings[2];

        splitter.md = {
            e,
            offsetLeft:  splitter.offsetLeft,
            offsetTop:   splitter.offsetTop,
            firstWidth:  first.offsetWidth,
            secondWidth: second.offsetWidth
        };

        window.addEventListener("mousemove", splitter.onmousemove);
        window.addEventListener("mouseup", splitter.onmousemove);

    }

    splitter.onmouseup = function(e) {

        splitter.md = null;

        window.removeEventListener("mousemove", splitter.onmousemove);
        window.removeEventListener("mouseup", splitter.onmousemove);

        splitter.dispatchEvent(new Event("resize"));
    }

    splitter.onmousemove = function(e) {

        if(!splitter.md) return;

        const first = siblings[0];
        const second = siblings[2];

        const delta = {
            x: e.clientX - splitter.md.e.clientX,
            y: e.clientY - splitter.md.e.clientY
        };

        // Prevent negative-sized elements
        delta.x = Math.min(Math.max(delta.x, -splitter.md.firstWidth), splitter.md.secondWidth);

        splitter.style.left = splitter.md.offsetLeft + delta.x + "px";
        first.style.width = (splitter.md.firstWidth + delta.x) + "px";
        second.style.width = (splitter.md.secondWidth - delta.x) + "px";

        // console.log("splitter.style.left=" + splitter.style.left);
        // console.log("first.style.width=" + first.style.width);
        // console.log("second.style.width=" + second.style.width);
        
    }
    
    
}