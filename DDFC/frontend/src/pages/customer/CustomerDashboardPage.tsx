import React, { useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { Plus, FileText, ChevronRight } from 'lucide-react';
import { useAppDispatch, useAppSelector } from '../../store/hooks';
import { fetchMyRequests } from '../../store/slices/requestsSlice';
import { fetchNotifications } from '../../store/slices/notificationsSlice';
import { Card, StatCard } from '../../components/ui/Card';
import { Button } from '../../components/ui/Button';
import { WorkflowStepper } from '../../components/shared/WorkflowStepper';
import { StatusBadge } from '../../components/shared/StatusBadge';
import { PageLoader } from '../../components/ui/LoadingSpinner';
import { format } from 'date-fns';

export const CustomerDashboardPage: React.FC = () => {
  const dispatch = useAppDispatch();
  const navigate = useNavigate();
  const { requests: myRequests, loading } = useAppSelector((s) => s.requests);
  const { unreadCount, notifications } = useAppSelector((s) => s.notifications);
  const customer = useAppSelector((s) => s.auth.customer);

  useEffect(() => {
    dispatch(fetchMyRequests());
    dispatch(fetchNotifications(undefined));
  }, [dispatch]);

  if (loading) return <PageLoader />;

  const latestRequest = myRequests[0];

  return (
    <div className="max-w-4xl mx-auto space-y-6 p-4">
      {/* Welcome section */}
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold text-gray-900">My Dashboard</h1>
        <Button
          variant="primary"
          onClick={() => navigate('/portal/requests/new')}
          icon={<Plus size={16} />}
        >
          New Request
        </Button>
      </div>

      <div className="grid grid-cols-2 sm:grid-cols-3 gap-4">
        <StatCard
          title="Total Requests"
          value={myRequests.length}
          color="blue"
        />
        <StatCard
          title="Unread Notifications"
          value={unreadCount}
          color={unreadCount > 0 ? 'red' : 'green'}
        />
        <StatCard
          title="Active Requests"
          value={myRequests.filter((r) => r.status !== 'Delivered' && r.status !== 'Rejected').length}
          color="green"
        />
      </div>

      {/* Active request stepper */}
      {latestRequest && latestRequest.status !== 'Delivered' && (
        <Card title="Request Progress" action={
          <button
            className="text-sm text-dha-green hover:underline"
            onClick={() => navigate(`/portal/requests/${latestRequest.requestId}`)}
          >
            View Details
          </button>
        }>
          <p className="text-sm text-gray-500 mb-4">
            Request ID:{' '}
            <span className="font-mono font-medium text-gray-900">
              {latestRequest.requestId}
            </span>
          </p>
          <WorkflowStepper currentStatus={latestRequest.status} requestType={latestRequest.requestType} />
        </Card>
      )}

      {/* Recent notifications */}
      {notifications.slice(0, 3).length > 0 && (
        <Card
          title="Recent Notifications"
          action={
            <button
              className="text-sm text-dha-green hover:underline"
              onClick={() => navigate('/portal/notifications')}
            >
              View All
            </button>
          }
        >
          <div className="space-y-3">
            {notifications.slice(0, 3).map((n) => (
              <div key={n.notificationId} className={`p-3 rounded-lg ${n.isRead ? 'bg-gray-50' : 'bg-green-50 border border-green-100'}`}>
                <p className="text-sm text-gray-800">{n.messageBody}</p>
                <p className="text-xs text-gray-400 mt-1">
                  {format(new Date(n.sentAt), 'dd MMM yyyy HH:mm')}
                </p>
              </div>
            ))}
          </div>
        </Card>
      )}

      {/* Requests list */}
      <Card title="All My Requests">
        {myRequests.length === 0 ? (
          <div className="text-center py-12 text-gray-400">
            <FileText size={40} className="mx-auto mb-3 opacity-30" />
            <p>No requests yet.</p>
            <Button
              variant="primary"
              className="mt-4"
              onClick={() => navigate('/portal/requests/new')}
              icon={<Plus size={16} />}
            >
              Submit First Request
            </Button>
          </div>
        ) : (
          <div className="space-y-2">
            {myRequests.map((req) => (
              <div
                key={req.requestId}
                className="flex items-center justify-between p-3 bg-gray-50 hover:bg-gray-100 rounded-lg cursor-pointer transition-colors"
                onClick={() => navigate(`/portal/requests/${req.requestId}`)}
              >
                <div>
                  <p className="font-mono font-medium text-sm text-gray-900">{req.requestId}</p>
                  <p className="text-xs text-gray-400">
                    Plot {req.plotNumber} • {format(new Date(req.submittedAt), 'dd MMM yyyy')}
                  </p>
                </div>
                <div className="flex items-center gap-2">
                  <StatusBadge status={req.status} />
                  <ChevronRight size={16} className="text-gray-400" />
                </div>
              </div>
            ))}
          </div>
        )}
      </Card>
    </div>
  );
};
