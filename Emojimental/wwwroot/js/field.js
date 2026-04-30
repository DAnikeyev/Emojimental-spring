window.emojimentalField = {
    getBounds(element) {
        const rect = element.getBoundingClientRect();
        return {
            left: rect.left,
            top: rect.top,
            width: rect.width,
            height: rect.height
        };
    },

    observeSize(element, dotNetRef) {
        if (!element) {
            return;
        }

        this.disposeSizeObserver(element);

        const notify = () => {
            const rect = element.getBoundingClientRect();
            dotNetRef.invokeMethodAsync("OnSurfaceResized", rect.width, rect.height);
        };

        const observer = new ResizeObserver(() => notify());
        observer.observe(element);
        element.__emojimentalFieldObserver = observer;
        notify();
    },

    disposeSizeObserver(element) {
        const observer = element?.__emojimentalFieldObserver;
        if (!observer) {
            return;
        }

        observer.disconnect();
        delete element.__emojimentalFieldObserver;
    }
};

