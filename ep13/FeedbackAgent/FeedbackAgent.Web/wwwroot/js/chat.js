let autoScroll = true;
let onUserScrollRegistered = false;
let isProgrammaticScroll = false;
let lastScrollTime;

export function scrollToBottom(elementId) {
    if (autoScroll) {
        const element = document.getElementById(elementId);
        lastScrollTime = new Date().getTime();
        element.scrollTop = element.scrollHeight;
    }
}

export function activateScrollToBottom(elementId) {
    autoScroll = true;

    if (!onUserScrollRegistered) {
        const chatElement = document.getElementById(elementId);
        chatElement.addEventListener("scroll", () => onUserScroll(elementId));
        onUserScrollRegistered = true;
        isProgrammaticScroll = true;
    }
}

function onUserScroll() {

    if (new Date().getTime() > lastScrollTime + 500)
        autoScroll = false;
}