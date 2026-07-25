import React, { useEffect } from 'react';
import {
  BarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
  LineChart,
  Line,
} from 'recharts';
import {
  FileText,
  Activity,
  CreditCard,
  CheckCircle,
  AlertTriangle,
} from 'lucide-react';
import { useAppDispatch, useAppSelector } from '../../store/hooks';
import { fetchAdminDashboard } from '../../store/slices/adminSlice';
import { StatCard } from '../../components/ui/Card';
import { Card } from '../../components/ui/Card';
import { PageLoader } from '../../components/ui/LoadingSpinner';
import { Badge } from '../../components/ui/Badge';
import { StatusBadge } from '../../components/shared/StatusBadge';
import { format } from 'date-fns';

export const AdminDashboardPage: React.FC = () => {
  const dispatch = useAppDispatch();
  const { dashboard, loading } = useAppSelector((s) => s.admin);

  useEffect(() => {
    dispatch(fetchAdminDashboard());
  }, [dispatch]);

  if (loading && !dashboard) return <PageLoader />;

  return (
    <div className="space-y-6">
      {/* Header */}
      <div>
        <h1 className="text-2xl font-bold text-gray-900">Admin Dashboard</h1>
        <p className="text-gray-500 text-sm mt-1">
          System-wide overview — {format(new Date(), 'EEEE, d MMMM yyyy')}
        </p>
      </div>

      {/* KPI Cards */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        <StatCard
          title="Total Requests Today"
          value={dashboard?.totalRequestsToday ?? '—'}
          icon={<FileText size={22} />}
          color="blue"
        />
        <StatCard
          title="Active Requests"
          value={dashboard?.activeRequests ?? '—'}
          icon={<Activity size={22} />}
          color="yellow"
        />
        <StatCard
          title="Pending Payments"
          value={dashboard?.pendingPayments ?? '—'}
          icon={<CreditCard size={22} />}
          color="red"
        />
        <StatCard
          title="Delivered This Month"
          value={dashboard?.deliveredThisMonth ?? '—'}
          icon={<CheckCircle size={22} />}
          color="green"
        />
      </div>

      {/* Charts row */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* Request pipeline chart */}
        <Card title="Request Pipeline" subtitle="Count per workflow state">
          {dashboard?.requestsByStatus && dashboard.requestsByStatus.length > 0 ? (
            <ResponsiveContainer width="100%" height={220}>
              <BarChart data={dashboard.requestsByStatus}>
                <CartesianGrid strokeDasharray="3 3" stroke="#f0f0f0" />
                <XAxis
                  dataKey="status"
                  tick={{ fontSize: 11 }}
                  angle={-30}
                  textAnchor="end"
                  height={50}
                />
                <YAxis tick={{ fontSize: 11 }} />
                <Tooltip />
                <Bar dataKey="count" fill="#3b82f6" radius={[4, 4, 0, 0]} />
              </BarChart>
            </ResponsiveContainer>
          ) : (
            <p className="text-gray-400 text-sm text-center py-10">No data available</p>
          )}
        </Card>

        {/* Department workload */}
        <Card title="Department Workload" subtitle="Pending tasks per department">
          {dashboard?.departmentWorkload && dashboard.departmentWorkload.length > 0 ? (
            <ResponsiveContainer width="100%" height={220}>
              <BarChart data={dashboard.departmentWorkload} layout="vertical">
                <CartesianGrid strokeDasharray="3 3" stroke="#f0f0f0" />
                <XAxis type="number" tick={{ fontSize: 11 }} />
                <YAxis dataKey="departmentName" type="category" tick={{ fontSize: 11 }} width={120} />
                <Tooltip />
                <Bar dataKey="pending" fill="#10b981" radius={[0, 4, 4, 0]} />
              </BarChart>
            </ResponsiveContainer>
          ) : (
            <p className="text-gray-400 text-sm text-center py-10">No data available</p>
          )}
        </Card>
      </div>

      {/* Avg turnaround + Recent activity */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* Avg turnaround time */}
        <Card title="Avg. Turnaround Time" subtitle="Rolling 30 days (days per stage)">
          {dashboard?.avgTurnaroundByStage && dashboard.avgTurnaroundByStage.length > 0 ? (
            <ResponsiveContainer width="100%" height={200}>
              <LineChart data={dashboard.avgTurnaroundByStage}>
                <CartesianGrid strokeDasharray="3 3" stroke="#f0f0f0" />
                <XAxis dataKey="stage" tick={{ fontSize: 11 }} />
                <YAxis tick={{ fontSize: 11 }} />
                <Tooltip />
                <Line
                  type="monotone"
                  dataKey="avgDays"
                  stroke="#8b5cf6"
                  strokeWidth={2}
                  dot={{ r: 4 }}
                />
              </LineChart>
            </ResponsiveContainer>
          ) : (
            <p className="text-gray-400 text-sm text-center py-10">No data available</p>
          )}
        </Card>

        {/* Recent activity feed */}
        <Card title="Recent Activity" subtitle="Last 20 state transitions">
          <div className="space-y-3 max-h-60 overflow-y-auto">
            {dashboard?.recentActivity?.length ? (
              dashboard.recentActivity.slice(0, 20).map((entry) => (
                <div
                  key={entry.historyId}
                  className="flex items-center justify-between gap-3 py-2 border-b border-gray-50 last:border-0"
                >
                  <div className="flex-1 min-w-0">
                    <p className="text-sm text-gray-800 truncate">
                      <span className="font-medium">{entry.actorName ?? 'System'}</span>
                      {' → '}
                      <StatusBadge status={entry.toStatus} />
                    </p>
                    {entry.comments && (
                      <p className="text-xs text-gray-400 truncate">{entry.comments}</p>
                    )}
                  </div>
                  <span className="text-xs text-gray-400 whitespace-nowrap">
                    {format(new Date(entry.timestamp), 'HH:mm')}
                  </span>
                </div>
              ))
            ) : (
              <p className="text-gray-400 text-sm text-center py-6">No recent activity</p>
            )}
          </div>
        </Card>
      </div>
    </div>
  );
};
