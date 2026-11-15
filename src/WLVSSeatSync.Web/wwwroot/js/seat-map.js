(() => {
    const form = document.getElementById("seat-map");
    if (!form || !window.signalR) return; // no live updates, but the page still works

    const eventId = form.dataset.eventId;
    const viewers = document.getElementById("viewer-count");
    const notice = document.getElementById("live-notice");

    const connection = new signalR.HubConnectionBuilder()
        .withUrl(form.dataset.hubUrl)
        .withAutomaticReconnect()
        .build();

    const showNotice = (text) => {
        notice.textContent = text;
        notice.classList.remove("d-none");
    };

    connection.on("SeatsChanged", (changedEventId, seatIds, isAvailable) => {
        if (changedEventId !== eventId) return;

        for (const id of seatIds) {
            const input = document.getElementById(`seat-${id}`);
            if (!input) continue;

            if (!isAvailable && input.checked) {
                input.checked = false;
                showNotice("A seat you picked was just taken by someone else. Please choose another.");
            }
            input.disabled = !isAvailable;

            const label = input.nextElementSibling;
            label?.classList.add("flash");
            setTimeout(() => label?.classList.remove("flash"), 1000);
        }
    });

    connection.on("ViewerCountChanged", (changedEventId, count) => {
        if (changedEventId !== eventId || !viewers) return;
        viewers.textContent = count <= 1
            ? "You're the only one looking at this event right now"
            : `${count} people are looking at this event right now`;
    });

    const start = async () => {
        try {
            await connection.start();
            await connection.invoke("WatchEvent", eventId);
        } catch (error) {
            console.warn("Live seat updates unavailable; retrying in 5s", error);
            setTimeout(start, 5000);
        }
    };

    // A reconnect is a brand-new connection, so it must join the event's group again.
    connection.onreconnected(() => connection.invoke("WatchEvent", eventId));
    start();
})();