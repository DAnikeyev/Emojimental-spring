window.emojimentalViewport = {
    observe(element, dotNetRef) {
        if (!element) {
            return;
        }

        this.dispose(element);

        const notify = () => {
            dotNetRef.invokeMethodAsync("OnViewportResized", element.clientWidth, element.clientHeight);
        };

        const observer = new ResizeObserver(() => notify());
        observer.observe(element);
        element.__emojimentalViewportObserver = observer;
        notify();
    },

    dispose(element) {
        const observer = element?.__emojimentalViewportObserver;
        if (!observer) {
            return;
        }

        observer.disconnect();
        delete element.__emojimentalViewportObserver;
    },

    setStyles(element, styles) {
        if (!element || !styles) return;
        element.style.cssText = styles;
    }
};