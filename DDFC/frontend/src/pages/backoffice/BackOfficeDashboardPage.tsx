import React, { useEffect } from 'react';
import { useAppDispatch, useAppSelector } from '../../store/hooks';
import {
  fetchMyTasks,
  fetchDepartmentTasks,
  fetchDepartmentWorkload,
} from '../../store/slices/tasksSlice';
import { StatCard, Card } from '../../components/ui/Card';
import { Badge } from '../../components/ui/Badge';
import { PageLoader } from '../../components/ui/LoadingSpinner';
import { StatusBadge } from '../../components/shared/StatusBadge';
import { ClipboardList, Clock, CheckCircle, AlertTriangle } from 'lucide-react';
import { useNavigate } from 'react-router-dom';
import { format } from 'date-fns';

export const BackOfficeDashboardPage: React.FC = () => {
  const dispatch = useAppDispatch();
  const navigate = useNavigate();
  const { myTasks, departmentTasks, workload, loading } = useAppSelector((s) => s.tasks);
  const user = useAppSelector((s) => s.auth.staffUser);
  const isManager = user?.roleName?.toLowerCase().includes('manager') ?? false;
  const deptId = user?.departmentId ?? '';

  useEffect(() => {
    dispatch(fetchMyTasks());
    if (isManager && deptId) {
      dispatch(fetchDepartmentTasks(deptId));
      dispatch(fetchDepartmentWorkload(deptId));
    }
  }, [dispatch, isManager, deptId]);

  const pending = myTasks.filter((t) => t.status === 'Pending').length;
  const inProgress = myTasks.filter((t) => t.status === 'InProgress').length;
  const completed = myTasks.filter((t) => t.status === 'Completed').length;
  const overdue = myTasks.filter(
    (t) => t.daysInQueue && t.daysInQueue > 5 && t.status !== 'Completed'
  ).length;

  if (loading) return <PageLoader />;

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-gray-900">
          {isManager ? 'Manager Dashboard' : 'My Tasks Dashboard'}
        </h1>
        <p className="text-gray-500 text-sm mt-1">
          {user?.departmentName} · {format(new Date(), 'EEEE, d MMMM yyyy')}
        </p>
      </div>

      {/* KPI Cards */}
      <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
        <StatCard
          title="Pending"
          value={isManager ? departmentTasks.filter((t) => t.status === 'Pending').length : pending}
          icon={<ClipboardList size={20} />}
          color="yellow"
        />
        <StatCard
          title="In Progress"
          value={isManager ? departmentTasks.filter((t) => t.status === 'InProgress').length : inProgress}
          icon={<Clock size={20} />}
          color="blue"
        />
        <StatCard
          title="Completed Today"
          value={completed}
          icon={<CheckCircle size={20} />}
          color="green"
        />
        <StatCard
          title="Overdue"
          value={overdue}
          icon={<AlertTriangle size={20} />}
          color="red"
        />
      </div>

      {/* My Tasks */}
      {!isManager && (
        <Card title="My Assigned Tasks">
          {myTasks.length === 0 ? (
            <p className="text-gray-400 text-sm text-center py-8">
              No tasks assigned to you
            </p>
          ) : (
            <div className="space-y-2">
              {myTasks.map((task) => (
                <div
                  key={task.taskId}
                  className="flex items-center justify-between p-4 rounded-xl border border-gray-100 hover:border-blue-200 hover:bg-blue-50 cursor-pointer transition-colors"
                  onClick={() =>
                    navigate(`/backoffice/requests/${task.requestId}`)
                  }
                >
                  <div>
                    <div className="flex items-center gap-2">
                      <span className="font-medium text-gray-900 text-sm">
                        {task.request?.requestId ?? task.requestId}
                      </span>
                      <StatusBadge
                        status={task.request?.status ?? 'Submitted'}
                      />
                    </div>
                    <p className="text-xs text-gray-500 mt-0.5">
                      {task.request?.customerName} ·{' '}
                      {task.request?.plotNumber ? `Plot ${task.request.plotNumber}` : ''}
                    </p>
                  </div>
                  <div className="text-right">
                    <Badge
                      variant={
                        task.status === 'Completed'
                          ? 'success'
                          : task.status === 'InProgress'
                          ? 'info'
                          : 'warning'
                      }
                    >
                      {task.status}
                    </Badge>
                    {task.daysInQueue && (
                      <p className="text-xs text-gray-400 mt-1">
                        {task.daysInQueue}d in queue
                      </p>
                    )}
                  </div>
                </div>
              ))}
            </div>
          )}
        </Card>
      )}

      {/* Manager: employee workload table */}
      {isManager && workload.length > 0 && (
        <Card title="Employee Workload">
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-gray-100">
                  <th className="text-left font-medium text-gray-500 pb-3">Employee</th>
                  <th className="text-center font-medium text-gray-500 pb-3">Assigned</th>
                  <th className="text-center font-medium text-gray-500 pb-3">In Progress</th>
                  <th className="text-center font-medium text-gray-500 pb-3">Completed Today</th>
                  <th className="text-center font-medium text-gray-500 pb-3">Status</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-50">
                {workload.map((emp) => (
                  <tr key={emp.userId} className="hover:bg-gray-50">
                    <td className="py-3 font-medium text-gray-900">{emp.employeeName}</td>
                    <td className="py-3 text-center">{emp.assigned}</td>
                    <td className="py-3 text-center">{emp.inProgress}</td>
                    <td className="py-3 text-center">{emp.completedToday}</td>
                    <td className="py-3 text-center">
                      <Badge variant={emp.isAvailable ? 'success' : 'default'}>
                        {emp.isAvailable ? 'Available' : 'Unavailable'}
                      </Badge>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </Card>
      )}
    </div>
  );
};
