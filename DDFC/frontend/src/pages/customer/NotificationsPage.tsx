import React, { useEffect } from 'react';
import { useAppDispatch, useAppSelector } from '../../store/hooks';
import { fetchNotifications, markNotificationRead } from '../../store/slices/notificationsSlice';
import { Card } from '../../components/ui/Card';
import { Badge } from '../../components/ui/Badge';
import { PageLoader } from '../../components/ui/LoadingSpinner';
import { Bell } from 'lucide-react';
import { format } from 'date-fns';

export const NotificationsPage: React.FC = () => {
  const dispatch = useAppDispatch();
  const { notifications, loading } = useAppSelector((s) => s.notifications);
  const customer = useAppSelector((s) => s.auth.customer);

  useEffect(() => {
    dispatch(fetchNotifications(undefined));
  }, [dispatch]);

  const handleRead = (id: string) => {
    dispatch(markNotificationRead(id));
  };

  if (loading) return <PageLoader />;

  return (
    <div className="max-w-3xl mx-auto p-4 space-y-5">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold text-gray-900">Notifications</h1>
        <Badge variant="secondary">{notifications.length}</Badge>
      </div>

      <Card>
        {notifications.length === 0 ? (
          <div className="text-center py-12 text-gray-400">
            <Bell size={36} className="mx-auto mb-2 opacity-30" />
            <p>No notifications yet</p>
          </div>
        ) : (
          <div className="divide-y divide-gray-50">
            {notifications.map((n) => (
              <div
                key={n.notificationId}
                className={`py-4 cursor-pointer hover:bg-gray-50 transition-colors ${
                  !n.isRead ? 'bg-green-50' : ''
                }`}
                onClick={() => !n.isRead && handleRead(n.notificationId)}
              >
                <div className="flex items-start justify-between gap-3">
                  <div className="flex-1">
                    {!n.isRead && (
                      <Badge variant="success" size="sm" className="mb-1">
                        New
                      </Badge>
                    )}
                    <p className="text-sm text-gray-800">{n.messageBody}</p>
                    {n.channel === 'SMS' && (
                      <p className="text-xs text-gray-400 mt-0.5">Sent via SMS</p>
                    )}
                    {n.channel === 'Email' && (
                      <p className="text-xs text-gray-400 mt-0.5">Sent via Email</p>
                    )}
                  </div>
                  <span className="text-xs text-gray-400 whitespace-nowrap">
                    {format(new Date(n.sentAt), 'dd MMM, HH:mm')}
                  </span>
                </div>
                {n.requiresResponse && !n.responseText && (
                  <div className="mt-2 p-2 bg-amber-50 border border-amber-200 rounded text-xs text-amber-700">
                    Response required — please visit the relevant request to respond.
                  </div>
                )}
              </div>
            ))}
          </div>
        )}
      </Card>
    </div>
  );
};
