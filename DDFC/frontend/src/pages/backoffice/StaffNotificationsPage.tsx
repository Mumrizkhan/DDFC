import React, { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { toast } from 'react-toastify';
import { Card } from '../../components/ui/Card';
import api from '../../services/api';

interface StaffNotification {
  id: string;
  requestId: string;
  title: string;
  messageBody: string;
  sentAt: string;
  isRead: boolean;
}

export const StaffNotificationsPage: React.FC = () => {
  const navigate = useNavigate();
  const [notifications, setNotifications] = useState<StaffNotification[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    api.get<StaffNotification[]>('/staff-notifications')
      .then((response) => setNotifications(response.data))
      .catch(() => toast.error('Unable to load notifications'))
      .finally(() => setLoading(false));
  }, []);

  const open = async (notification: StaffNotification) => {
    if (!notification.isRead) {
      try {
        await api.post(`/staff-notifications/${notification.id}/read`);
        window.dispatchEvent(new Event('staff-notifications-changed'));
      } catch {
        toast.error('Unable to mark notification as read');
        return;
      }
    }
    navigate(`/backoffice/requests/${notification.requestId}`);
  };

  return (
    <div className="max-w-3xl space-y-5">
      <h1 className="text-2xl font-bold text-gray-900">Notifications</h1>
      <Card>
        {loading ? <p className="text-sm text-gray-500">Loading notifications...</p> : notifications.length === 0 ? (
          <p className="text-sm text-gray-500">No notifications yet.</p>
        ) : (
          <div className="divide-y divide-gray-100">
            {notifications.map((notification) => (
              <button key={notification.id} type="button" onClick={() => void open(notification)}
                className={`w-full text-left py-4 px-2 hover:bg-gray-50 ${notification.isRead ? '' : 'bg-emerald-50'}`}>
                <div className="flex flex-wrap justify-between gap-2">
                  <span className="text-sm font-semibold text-gray-900">{notification.title}{!notification.isRead && ' (new)'}</span>
                  <span className="text-xs text-gray-500">{new Date(notification.sentAt).toLocaleString()}</span>
                </div>
                <p className="text-sm text-gray-700 mt-1 break-words">{notification.messageBody}</p>
              </button>
            ))}
          </div>
        )}
      </Card>
    </div>
  );
};