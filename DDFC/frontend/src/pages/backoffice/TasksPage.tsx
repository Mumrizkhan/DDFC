import React, { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Clock, AlertCircle } from 'lucide-react';
import { useAppDispatch, useAppSelector } from '../../store/hooks';
import { fetchMyTasks } from '../../store/slices/tasksSlice';
import { Card } from '../../components/ui/Card';
import { StatusBadge } from '../../components/shared/StatusBadge';
import { Badge } from '../../components/ui/Badge';
import { PageLoader } from '../../components/ui/LoadingSpinner';
import { differenceInDays, format } from 'date-fns';
import { draftsmanTasksService, type DraftsmanTask } from '../../services/draftsmanTasks';
import { toast } from 'react-toastify';

export const TasksPage: React.FC = () => {
  const dispatch = useAppDispatch();
  const navigate = useNavigate();
  const { myTasks, loading } = useAppSelector((s) => s.tasks);
  const [draftsmanTasks, setDraftsmanTasks] = useState<DraftsmanTask[]>([]);

  useEffect(() => {
    dispatch(fetchMyTasks());
    draftsmanTasksService.mine().then(setDraftsmanTasks).catch(() => toast.error('Unable to load Drafts Man tasks'));
  }, [dispatch]);

  if (loading) return <PageLoader />;

  const pending = myTasks.filter((t) => !t.isCompleted);
  const completed = myTasks.filter((t) => t.isCompleted);
  const pendingDrafts = draftsmanTasks.filter((task) => !task.completedAt);
  const completedDrafts = draftsmanTasks.filter((task) => task.completedAt);

  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-bold text-gray-900">My Tasks</h1>

      <div className="grid gap-4">
        <h2 className="text-sm font-semibold uppercase tracking-wider text-gray-500">
          Pending ({pending.length + pendingDrafts.length})
        </h2>
        {pending.length === 0 && pendingDrafts.length === 0 && (
          <Card>
            <p className="text-gray-400 text-center py-6">No pending tasks</p>
          </Card>
        )}
        {pendingDrafts.map((task) => (
          <button key={task.id} type="button" onClick={() => navigate(`/backoffice/draftsman-tasks/${task.id}`)}
            className="w-full text-left bg-white border border-gray-200 rounded-lg p-4 hover:shadow-md">
            <p className="font-medium text-gray-900">{task.requestNumber}</p>
            <p className="text-sm text-gray-500">Drafts Man &middot; {task.parentStep === 'ThreeD' ? '3D Visualization' : task.parentStep}</p>
          </button>
        ))}
        {pending.map((task) => {
          const daysOld = differenceInDays(new Date(), new Date(task.assignedAt ?? new Date()));
          const isOverdue = daysOld > 3;
          return (
            <div
              key={task.taskId}
              className={`bg-white border rounded-lg p-4 cursor-pointer hover:shadow-md transition-shadow ${isOverdue ? 'border-red-200' : 'border-gray-200'}`}
              onClick={() => navigate(`/backoffice/requests/${task.requestId}`)}
            >
              <div className="flex items-center justify-between">
                <div className="flex items-center gap-3">
                  {isOverdue && <AlertCircle size={16} className="text-red-500 shrink-0" />}
                  <div>
                    <p className="font-medium text-gray-900">{task.requestId}</p>
                    <p className="text-sm text-gray-500">{task.taskType}</p>
                  </div>
                </div>
                <div className="flex items-center gap-3">
                  {task.requestStatus && <StatusBadge status={task.requestStatus} />}
                  <span className="flex items-center gap-1 text-xs text-gray-400">
                    <Clock size={12} />
                    {task.assignedAt ? format(new Date(task.assignedAt), 'dd MMM') : '—'}
                  </span>
                  {isOverdue && (
                    <Badge variant="danger" size="sm">
                      {daysOld}d overdue
                    </Badge>
                  )}
                </div>
              </div>
            </div>
          );
        })}

        {(completed.length > 0 || completedDrafts.length > 0) && (
          <>
            <h2 className="text-sm font-semibold uppercase tracking-wider text-gray-500 mt-4">
              Completed ({completed.length + completedDrafts.length})
            </h2>
            {completedDrafts.map((task) => (
              <button key={task.id} type="button" onClick={() => navigate(`/backoffice/draftsman-tasks/${task.id}`)}
                className="w-full text-left bg-gray-50 border border-gray-100 rounded-lg p-4 hover:bg-gray-100">
                <p className="font-medium text-gray-700">{task.requestNumber}</p>
                <p className="text-sm text-gray-500">Drafts Man &middot; {task.parentStep === 'ThreeD' ? '3D Visualization' : task.parentStep}</p>
              </button>
            ))}
            {completed.map((task) => (
              <div
                key={task.taskId}
                className="bg-gray-50 border border-gray-100 rounded-lg p-4 cursor-pointer hover:bg-gray-100 transition-colors"
                onClick={() => navigate(`/backoffice/requests/${task.requestId}`)}
              >
                <div className="flex items-center justify-between">
                  <div>
                    <p className="font-medium text-gray-700">{task.requestId}</p>
                    <p className="text-sm text-gray-400">{task.taskType}</p>
                  </div>
                  <div className="flex items-center gap-3">
                    {task.requestStatus && <StatusBadge status={task.requestStatus} />}
                    <span className="text-xs text-gray-400">
                      {task.completedAt
                        ? format(new Date(task.completedAt), 'dd MMM yyyy')
                        : ''}
                    </span>
                  </div>
                </div>
              </div>
            ))}
          </>
        )}
      </div>
    </div>
  );
};
