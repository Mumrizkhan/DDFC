import React, { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { ArrowLeft, CheckCircle } from 'lucide-react';
import { toast } from 'react-toastify';
import { Button } from '../../components/ui/Button';
import { Card } from '../../components/ui/Card';
import { FileUploadButton } from '../../components/ui/FileUploadButton';
import { draftsmanTasksService, type DraftsmanTask } from '../../services/draftsmanTasks';

export const DraftsmanTaskPage: React.FC = () => {
  const { taskId } = useParams<{ taskId: string }>();
  const navigate = useNavigate();
  const [task, setTask] = useState<DraftsmanTask | null>(null);
  const [fileUrl, setFileUrl] = useState('');
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    if (!taskId) return;
    draftsmanTasksService.get(taskId).then(setTask)
      .catch(() => toast.error('Unable to load Drafts Man step'))
      .finally(() => setLoading(false));
  }, [taskId]);

  const complete = async () => {
    if (!taskId || !fileUrl) return;
    setSaving(true);
    try {
      setTask(await draftsmanTasksService.complete(taskId, fileUrl));
      setFileUrl('');
      toast.success('Drafts Man step completed');
    } catch (error) {
      toast.error((error as { message?: string }).message ?? 'Unable to complete Drafts Man step');
    } finally { setSaving(false); }
  };

  if (loading) return <p className="text-sm text-gray-500">Loading...</p>;
  if (!task) return <p className="text-sm text-gray-500">Drafts Man step not found.</p>;

  return (
    <div className="max-w-3xl space-y-4">
      <button type="button" onClick={() => navigate('/backoffice/tasks')} className="flex items-center gap-2 text-sm text-gray-600 hover:text-gray-900">
        <ArrowLeft size={16} /> My Tasks
      </button>
      <Card title="Drafts Man">
        <div className="space-y-4">
          <div className="text-sm text-gray-700 space-y-1">
            <p><span className="font-medium">Request:</span> {task.requestNumber}</p>
            <p><span className="font-medium">Parent step:</span> {task.parentStep === 'ThreeD' ? '3D Visualization' : task.parentStep}</p>
            <p><span className="font-medium">Status:</span> {task.completedAt ? 'Completed' : 'Pending'}</p>
          </div>
          <Button type="button" variant="outline" onClick={() => navigate(`/backoffice/requests/${task.requestId}?tab=Documents`)}>
            View Request Documents
          </Button>
          {task.fileUrl && <a href={task.fileUrl} target="_blank" rel="noopener noreferrer" className="text-sm text-blue-700 underline break-all">View submitted document</a>}
          {!task.completedAt && (
            <>
              <FileUploadButton label="Drawing" required accept=".dwg,.pdf" uploadText="Click to upload DWG / PDF" value={fileUrl} onUploaded={setFileUrl} />
              <Button onClick={complete} disabled={!fileUrl} loading={saving} icon={<CheckCircle size={16} />}>Complete Drafts Man Step</Button>
            </>
          )}
        </div>
      </Card>
    </div>
  );
};