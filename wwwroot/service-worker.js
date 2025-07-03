console.log('NEW Service Worker loaded');

self.addEventListener('push', function(event) {
    console.log('=== NEW SW PUSH EVENT ===');
    console.log('Data exists:', !!event.data);
    
    if (!event.data) {
        console.log('No push data');
        return;
    }
    
    console.log('Data object:', event.data);
    console.log('Data constructor:', event.data.constructor.name);
    console.log('Available methods:', Object.getOwnPropertyNames(Object.getPrototypeOf(event.data)));
    
    event.waitUntil(
        (async () => {
            try {
                let data;
                
                // Try to get the data using json() method first
                if (typeof event.data.json === 'function') {
                    console.log('Using json() method');
                    data = await event.data.json();
                } else if (typeof event.data.arrayBuffer === 'function') {
                    console.log('Using arrayBuffer() method');
                    const buffer = await event.data.arrayBuffer();
                    const decoder = new TextDecoder();
                    const text = decoder.decode(buffer);
                    console.log('Decoded text:', text);
                    data = JSON.parse(text);
                } else if (typeof event.data.text === 'function') {
                    console.log('Using text() method');
                    const text = await event.data.text();
                    console.log('Text data:', text);
                    data = JSON.parse(text);
                } else {
                    throw new Error('No data access methods available');
                }
                
                console.log('Parsed notification data:', data);
                
                const options = {
                    body: data.body || 'Default body',
                    icon: data.icon || '/icon.png',
                    badge: data.badge || '/badge.png',
                    tag: data.tag,
                    requireInteraction: data.requireInteraction || false,
                    data: data.data,
                    actions: data.actions || []
                };
                
                console.log('Final notification options:', options);
                console.log('Actions count:', options.actions.length);
                
                if (options.actions.length > 0) {
                    console.log('Action buttons:');
                    options.actions.forEach((action, i) => {
                        console.log(`  ${i}: ${action.title} (${action.action})`);
                    });
                }
                
                return self.registration.showNotification(
                    data.title || 'Notification',
                    options
                );
            } catch (error) {
                console.error('Error processing push data:', error);
                console.error('Error stack:', error.stack);
                
                // Show basic notification without actions
                return self.registration.showNotification('Notification Error', {
                    body: 'Unable to process notification data: ' + error.message,
                    icon: '/icon.png'
                });
            }
        })()
    );
});

self.addEventListener('notificationclick', function(event) {
    console.log('Notification clicked');
    console.log('Action:', event.action);
    console.log('Notification data:', event.notification.data);
    
    if (event.action && event.action.startsWith('select_')) {
        const selectedMeetingId = event.action.replace('select_', '');
        const conflictId = event.notification.data.conflictId;
        
        console.log('User selected meeting:', selectedMeetingId);
        console.log('For conflict:', conflictId);
        
        event.waitUntil(
            fetch('/MeetingChoice', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    ConflictId: conflictId,
                    SelectedMeetingId: selectedMeetingId
                })
            }).then(response => {
                if (response.ok) {
                    console.log('Choice saved successfully');
                    // Show confirmation notification
                    return self.registration.showNotification('Meeting Selected', {
                        body: `You chose ${selectedMeetingId}. You'll receive reminders only for this meeting.`,
                        icon: '/icon.png',
                        tag: 'choice-confirmation'
                    });
                } else {
                    console.error('Failed to save choice');
                }
            }).catch(error => {
                console.error('Failed to save choice:', error);
            })
        );
    }
    
    event.notification.close();
});

self.addEventListener('notificationclose', function(event) {
    console.log('Notification closed');
});