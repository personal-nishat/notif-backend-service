
const publicVapidKey = 'BBJcNtnGYKR_RP6He5csg1WmSyB98qFebmigdSnrsQFYOVJqd7u-Q0nxqvY0uXg7m1jFN9lysrCO3pseWaq7LlE'; // Replace with your actual VAPID public key
console.log("HI working");


        if ('serviceWorker' in navigator && 'PushManager' in window) {
            navigator.serviceWorker.register('service-worker.js')
                .then(function(registration) {
                    console.log('Service Worker registered:', registration);
                })
                .catch(function(error) {
                    console.error('Service Worker registration failed:', error);
                });

            document.getElementById('subscribe').addEventListener('click', async () => {
                const registration = await navigator.serviceWorker.ready;

                const subscription = await registration.pushManager.subscribe({
                    userVisibleOnly: true,
                    applicationServerKey: urlBase64ToUint8Array(publicVapidKey)
                });

                await fetch('/subscription', {
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

// Helper function to convert ArrayBuffer to base64
        function arrayBufferToBase64(buffer) {
                return btoa(String.fromCharCode.apply(null, new Uint8Array(buffer)));
            }

                alert('Subscribed to push notifications!');
            });
        } else {
            alert('Push messaging is not supported in your browser.');
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