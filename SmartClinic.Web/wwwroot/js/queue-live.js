// Keeps a queue page live: SignalR pushes "QueueChanged", we re-fetch the page's own HTML partial.
// A 20-second timer is the fallback if the real-time connection is unavailable.
(function () {
    var box = document.getElementById('liveRegion');
    if (!box) return;
    var url = box.getAttribute('data-refresh-url');
    var busy = false;

    function refresh() {
        if (busy) return;
        busy = true;
        fetch(url, { credentials: 'same-origin', headers: { 'X-Requested-With': 'XMLHttpRequest' } })
            .then(function (r) { return r.ok && r.status !== 204 ? r.text() : null; })
            .then(function (html) { if (html) box.innerHTML = html; })
            .catch(function () { /* keep the last good view */ })
            .finally(function () { busy = false; });
    }

    setInterval(refresh, 20000);

    if (window.signalR) {
        var conn = new signalR.HubConnectionBuilder().withUrl('/hubs/queue').withAutomaticReconnect().build();
        conn.on('QueueChanged', refresh);
        conn.onreconnected(function () { box.setAttribute('data-live', 'on'); refresh(); });
        conn.onreconnecting(function () { box.removeAttribute('data-live'); });
        conn.onclose(function () { box.removeAttribute('data-live'); });
        conn.start().then(function () { box.setAttribute('data-live', 'on'); }).catch(function () { box.removeAttribute('data-live'); });
    }
})();
