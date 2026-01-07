window.immoPush = {
    async register(publicKey, apiBase) {
        if (!('serviceWorker' in navigator)) {
            console.warn('Service workers not supported');
            return false;
        }
        if (!('PushManager' in window)) {
            console.warn('Push not supported');
            return false;
        }
        const permission = await Notification.requestPermission();
        if (permission !== 'granted') {
            console.warn('Notification permission denied');
            return false;
        }
        const reg = await navigator.serviceWorker.register('/sw.js');
        const sub = await reg.pushManager.subscribe({
            userVisibleOnly: true,
            applicationServerKey: urlBase64ToUint8Array(publicKey)
        });
        const payload = {
            endpoint: sub.endpoint,
            p256dh: arrayBufferToBase64(sub.getKey('p256dh')),
            auth: arrayBufferToBase64(sub.getKey('auth'))
        };
        const response = await fetch(`/api/notifications/webpush/subscribe`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        });
        return response.ok;
    }
};

function urlBase64ToUint8Array(base64String) {
    const padding = '='.repeat((4 - (base64String.length % 4)) % 4);
    const base64 = (base64String + padding).replace(/-/g, '+').replace(/_/g, '/');
    const raw = atob(base64);
    const output = new Uint8Array(raw.length);
    for (let i = 0; i < raw.length; ++i) {
        output[i] = raw.charCodeAt(i);
    }
    return output;
}

function arrayBufferToBase64(buffer) {
    const bytes = new Uint8Array(buffer);
    let binary = '';
    for (let i = 0; i < bytes.byteLength; i++) {
        binary += String.fromCharCode(bytes[i]);
    }
    return btoa(binary);
}
