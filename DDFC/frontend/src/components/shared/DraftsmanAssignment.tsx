import React, { useEffect, useState } from 'react';
import { FileCheck, Users } from 'lucide-react';
import { toast } from 'react-toastify';
import { Button } from '../ui/Button';
import { Select } from '../ui/Input';
import api from '../../services/api';
import { draftsmanTasksService, type DraftsmanParent, type DraftsmanTask } from '../../services/draftsmanTasks';
import type { PossessionRequest } from '../../types';

interface StaffUser {
  userId: string;
  fullName: string;
  roleName?: string;
  departmentName?: string;
  isActive?: boolean;
}

interface Props {
  parentStep: DraftsmanParent;
  requestId: string;
  request: PossessionRequest;
}

export const DraftsmanAssignment: React.FC<Props> = ({ parentStep, requestId, request }) => {
  const [users, setUsers] = useState<StaffUser[]>([]);
  const [task, setTask] = useState<DraftsmanTask | null>(null);
  const [selected, setSelected] = useState('');
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const legacy = request.cadAssignments?.find((assignment) => assignment.cadType === parentStep && assignment.completedAt);

  useEffect(() => {
    let active = true;
    const refresh = () => draftsmanTasksService.forRequest(requestId)
      .then((tasks) => { if (active) setTask(tasks.find((item) => item.parentStep === parentStep) ?? null); })
      .catch(() => {});
    Promise.all([draftsmanTasksService.forRequest(requestId), api.get<StaffUser[] | { users: StaffUser[] }>('/admin/users')])
      .then(([tasks, response]) => {
        if (!active) return;
        setTask(tasks.find((item) => item.parentStep === parentStep) ?? null);
        const allUsers = Array.isArray(response.data) ? response.data : response.data.users ?? [];
        setUsers(allUsers.filter((user) => {
          const department = (user.departmentName ?? '').toLowerCase();
          return user.isActive !== false && (user.roleName?.toLowerCase() === '3d operator' || department.includes('3d') || department.includes('three d'));
        }));
      })
      .catch(() => { if (active) toast.error('Unable to load draftsman assignments'); })
      .finally(() => { if (active) setLoading(false); });
    const timer = window.setInterval(refresh, 30000);
    return () => { active = false; window.clearInterval(timer); };
  }, [requestId, parentStep]);

  const assign = async () => {
    if (!selected) return;
    setSaving(true);
    try {
      setTask(await draftsmanTasksService.assign(requestId, parentStep, selected));
      setSelected('');
      toast.success('Drafts Man child step assigned');
    } catch (error) {
      toast.error((error as { message?: string }).message ?? 'Unable to assign Drafts Man');
    } finally { setSaving(false); }
  };

  return (
    <div className="border rounded-lg p-4 space-y-3 border-gray-200 bg-gray-50">
      <h4 className="text-sm font-semibold text-gray-800">Drafts Man</h4>
      {loading ? <p className="text-sm text-gray-500">Loading...</p> : (
        <>
          {task && <p className="text-sm text-gray-700">{task.assignedToUserName} &middot; {task.completedAt ? 'Completed' : 'Pending'}</p>}
          {(task?.fileUrl || (!task && legacy?.fileUrl)) && (
            <a href={task?.fileUrl || legacy?.fileUrl} target="_blank" rel="noopener noreferrer" className="flex items-center gap-2 text-sm text-blue-700 underline break-all">
              <FileCheck size={16} className="shrink-0" /> View Drafts Man Document
            </a>
          )}
          <div className="flex gap-2 items-end">
            <div className="flex-1 min-w-0">
              <Select label="Assign Drafts Man" value={selected} onChange={(event) => setSelected(event.target.value)} options={[
                { value: '', label: users.length ? 'Select Drafts Man' : 'No draftsmen available' },
                ...users.map((user) => ({ value: user.userId, label: user.fullName })),
              ]} />
            </div>
            <Button type="button" variant="outline" loading={saving} disabled={!selected} onClick={assign} icon={<Users size={14} />}>
              {task ? 'Reassign' : 'Assign'}
            </Button>
          </div>
        </>
      )}
    </div>
  );
};