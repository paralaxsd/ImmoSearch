self.addEventListener('push', event => {
    console.log('Push event received:', event);
    event.waitUntil(handlePush(event));
});

async function handlePush(event) {
    console.log('Handling push event');
    let payload = {};
    if (event.data) {
        try {
            payload = event.data.json();
            console.log('Parsed JSON payload:', payload);
        } catch (e) {
            console.log('Failed to parse JSON, trying text:', e);
            try {
                const text = event.data.text();
                payload = text ? { body: text } : {};
                console.log('Parsed text payload:', payload);
            } catch (e2) {
                console.log('Failed to parse text:', e2);
                payload = {};
            }
        }
    } else {
        console.log('No event.data');
    }

    const title = payload.title || 'ImmoSearch';
    const options = {
        body: payload.body || '',
        data: { url: payload.url }
    };

    console.log('Showing notification:', title, options);
    try {
        await self.registration.showNotification(title, options);
        console.log('Notification shown successfully');
    } catch (error) {
        console.error('Failed to show notification:', error);
    }
}

self.addEventListener('notificationclick', event => {
    event.notification.close();
    const url = event.notification.data && event.notification.data.url;
    if (url) {
        event.waitUntil(clients.openWindow(url));
    }
});
