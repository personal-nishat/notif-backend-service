const publicVapidKey = 'BBJcNtnGYKR_RP6He5csg1WmSyB98qFebmigdSnrsQFYOVJqd7u-Q0nxqvY0uXg7m1jFN9lysrCO3pseWaq7LlE';
console.log("HI working");

// Helper function to convert ArrayBuffer to base64
function arrayBufferToBase64(buffer) {
    return btoa(String.fromCharCode.apply(null, new Uint8Array(buffer)));
}

function urlBase64ToUint8Array(base64String) {
    const padding = '='.repeat((4 - base64String.length % 4) % 4);
    const base64 = (base64String + padding)
        .replace(/-/g, '+')
        .replace(/_/g, '/');

    const rawData = window.atob(base64);
    const outputArray = new Uint8Array(rawData.length);

    for (let i = 0; i < rawData.length; ++i) {
        outputArray[i] = rawData.charCodeAt(i);
    }
    return outputArray;
}

if ('serviceWorker' in navigator && 'PushManager' in window) {
    navigator.serviceWorker.register('service-worker.js')
        .then(function(registration) {
            console.log('NEW Service Worker registered:', registration);
        })
        .catch(function(error) {
            console.error('Service Worker registration failed:', error);
        });

    document.getElementById('subscribe').addEventListener('click', async () => {
        try {
            const registration = await navigator.serviceWorker.ready;
            console.log('Service worker ready:', registration);

            const subscription = await registration.pushManager.subscribe({
                userVisibleOnly: true,
                applicationServerKey: urlBase64ToUint8Array(publicVapidKey)
            });

            console.log('Push subscription created:', subscription);

            const response = await fetch('/subscription', {
                method: 'POST',
                body: JSON.stringify({
                    Endpoint: subscription.endpoint,
                    Keys: {
                        P256dh: arrayBufferToBase64(subscription.getKey('p256dh')),
                        Auth: arrayBufferToBase64(subscription.getKey('auth'))
                    }
                }),
                headers: {
                    'Content-Type': 'application/json'
                }
            });

            if (response.ok) {
                alert('Subscribed to push notifications!');
                console.log('Subscription registered successfully');
            } else {
                alert('Failed to register subscription');
                console.error('Failed to register subscription:', response.status);
            }
        } catch (error) {
            console.error('Subscription error:', error);
            alert('Error subscribing: ' + error.message);
        }
    });
} else {
    alert('Push messaging is not supported in your browser.');
}