self.addEventListener('push', function(event) {
    let data = {};
    console.log('Push event received:', event);
    try {
        data = event.data.json();
    } catch (e) {
        data = { title: 'Notification', body: 'You have a new notification.' };
    }
    event.waitUntil(
        self.registration.showNotification(data.title || 'Notification', {
            body: data.body || '',
        })
    );
});
