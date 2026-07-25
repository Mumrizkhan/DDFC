import React, { useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { Inbox, CheckCircle, Clock, AlertCircle } from 'lucide-react';
import { useAppDispatch, useAppSelector } from '../../store/hooks';
import { fetchQueueTickets } from '../../store/slices/ticketsSlice';
import { Card } from '../../components/ui/Card';
import { PageLoader } from '../../components/ui/LoadingSpinner';

export const TechSupportDashboardPage: React.FC = () => {
  const dispatch = useAppDispatch();
  const navigate = useNavigate();
  const { tickets, loading } = useAppSelector((s) => s.tickets);
  const user = useAppSelector((s) => s.auth.staffUser);

  useEffect(() => {
    dispatch(fetchQueueTickets());
  }, [dispatch]);

  if (loading) return <PageLoader />;

  const open       = tickets.filter((t) => t.status === 'Open').length;
  const inProgress = tickets.filter((t) => t.status === 'InProgress').length;
  const resolved   = tickets.filter((t) => t.status === 'Resolved').length;
  const recent     = [...tickets].sort(
    (a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime()
  ).slice(0, 5);

  const stats = [
    { label: 'Open',        count: open,       icon: Inbox,       color: 'text-blue-500',   bg: 'bg-blue-50'   },
    { label: 'In Progress', count: inProgress,  icon: Clock,       color: 'text-amber-500',  bg: 'bg-amber-50'  },
    { label: 'Resolved',    count: resolved,    icon: CheckCircle, color: 'text-emerald-500', bg: 'bg-emerald-50'},
    { label: 'Total',       count: tickets.length, icon: AlertCircle, color: 'text-slate-500', bg: 'bg-slate-50' },
  ];

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-gray-900">Technical Support Dashboard</h1>
        <p className="text-gray-500 text-sm mt-1">
          Welcome back, {user?.fullName ?? 'Agent'}
        </p>
      </div>

      {/* Stats */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        {stats.map(({ label, count, icon: Icon, color, bg }) => (
          <Card key={label}>
            <div className="flex items-center gap-3">
              <div className={`w-10 h-10 rounded-lg ${bg} flex items-center justify-center`}>
                <Icon size={20} className={color} />
              </div>
              <div>
                <p className="text-2xl font-bold text-gray-900">{count}</p>
                <p className="text-xs text-gray-500">{label}</p>
              </div>
            </div>
          </Card>
        ))}
      </div>

      {/* Recent Tickets */}
      <div>
        <div className="flex items-center justify-between mb-3">
          <h2 className="text-base font-semibold text-gray-700">Recent Tickets</h2>
          <button
            className="text-sm text-emerald-600 hover:text-emerald-700 font-medium"
            onClick={() => navigate('/backoffice/tickets')}
          >
            View All →
          </button>
        </div>

        {recent.length === 0 ? (
          <Card>
            <p className="text-center text-gray-400 py-6">No tickets yet</p>
          </Card>
        ) : (
          <div className="space-y-2">
            {recent.map((ticket) => (
              <div
                key={ticket.ticketId}
                className="bg-white border border-gray-200 rounded-lg px-4 py-3 flex items-center justify-between cursor-pointer hover:shadow-sm transition-shadow"
                onClick={() => navigate(`/backoffice/tickets/${ticket.ticketId}`)}
              >
                <div className="min-w-0">
                  <p className="font-medium text-gray-900 text-sm truncate">{ticket.subject}</p>
                  <p className="text-xs text-gray-500">{ticket.category} · {ticket.customerName ?? 'Customer'}</p>
                </div>
                <StatusChip status={ticket.status} />
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
};

const statusColors: Record<string, string> = {
  Open:       'bg-blue-100 text-blue-700',
  InProgress: 'bg-amber-100 text-amber-700',
  Resolved:   'bg-emerald-100 text-emerald-700',
  Closed:     'bg-gray-100 text-gray-600',
};

const StatusChip: React.FC<{ status: string }> = ({ status }) => (
  <span className={`text-xs font-medium px-2 py-0.5 rounded-full ${statusColors[status] ?? 'bg-gray-100 text-gray-600'}`}>
    {status}
  </span>
);
