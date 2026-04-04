
mounted: function() {

    const splitter = this.$el;
    const siblings = splitter.parentElement.children;
    if(siblings.length !== 3) return;

    const first = siblings[0];
    const second = siblings[2];

    const directionCol = first.offsetLeft < second.offsetLeft;

    splitter.classList = directionCol 
        ? "st-splitter-col"
        : "st-splitter-row";
    
    splitter.onpointerdown = function(e) {

        splitter.md = {
            e,
            offsetLeft:  splitter.offsetLeft,
            offsetTop:   splitter.offsetTop,
            firstWidth:  first.offsetWidth,
            secondWidth: second.offsetWidth,
            firstHeight:  first.offsetHeight,
            secondHeight: second.offsetHeight
        };

        splitter.setPointerCapture(e.pointerId);
    }

    splitter.onpointerup = function(e) {

        if(!splitter.md) return;

        splitter.releasePointerCapture(e.pointerId);

        const params = directionCol
            ? { first: first.style.width.replace('px', ''), second: second.style.width.replace('px', '') }
            : { first: first.style.height.replace('px', ''), second: second.style.height.replace('px', '') };
        splitter.dispatchEvent(new CustomEvent("moved", { detail: params } ));

        splitter.md = null;
    }

    splitter.onpointermove = function(e) {

        if(!splitter.md) return;

        const delta = {
            x: e.clientX - splitter.md.e.clientX,
            y: e.clientY - splitter.md.e.clientY
        };

        if(directionCol) {

            // Prevent negative-sized elements
            delta.x = Math.min(Math.max(delta.x, -splitter.md.firstWidth), splitter.md.secondWidth);

            splitter.style.left = splitter.md.offsetLeft + delta.x + "px";
            first.style.width = (splitter.md.firstWidth + delta.x) + "px";
            second.style.width = (splitter.md.secondWidth - delta.x) + "px";

        } else {

            // Prevent negative-sized elements
            delta.y = Math.min(Math.max(delta.y, -splitter.md.firstHeight), splitter.md.secondHeight);

            splitter.style.top = splitter.md.offsetTop + delta.y + "px";
            first.style.height = (splitter.md.firstHeight + delta.y) + "px";
            second.style.height = (splitter.md.secondHeight - delta.y) + "px";

        }
        
    }
    
    
}