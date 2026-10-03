import React, { useState } from 'react';
import { CheckCircle, XCircle } from 'lucide-react';
import { Card } from '../../../components/ui/Card';
import { Button } from '../../../components/ui/Button';
import { useAppDispatch } from '../../../store/hooks';
import { adCoordApprove, adCoordReject } from '../../../store/slices/requestsSlice';
import { toast } from 'react-toastify';

type ActionMode = 'approve' | 'reject' | null;

interface Props {
  requestId: string;
}

export const AdCoordPanel: React.FC<Props> = ({ requestId }) => {
  const dispatch = useAppDispatch();
  const [actionMode, setActionMode] = useState<ActionMode>(null);
  const [comments, setComments] = useState('');
  const [submitting, setSubmitting] = useState(false);

  const handleApprove = async () => {
    setActionMode('approve');
    setSubmitting(true);
    try {
      await dispatch(adCoordApprove({ id: requestId, comments: comments.trim() || undefined })).unwrap();
      toast.success('Approved — request advanced to DDFC Admin');
      setComments('');
    } catch {
      toast.error('Failed to approve');
    } finally {
      setSubmitting(false);
      setActionMode(null);
    }
  };

  const handleReject = async () => {
    if (!comments.trim()) {
      toast.error('Please provide a reason for rejection');
      return;
    }
    setActionMode('reject');
    setSubmitting(true);
    try {
      await dispatch(adCoordReject({ id: requestId, comments: comments.trim() })).unwrap();
      toast.success('Request rejected');
      setComments('');
    } catch {
      toast.error('Failed to reject');
    } finally {
      setSubmitting(false);
      setActionMode(null);
    }
  };

  return (
    <Card title="AD Coordinator – Review">
      <div className="space-y-4">
        <p className="text-sm text-gray-500">
          Review the request now that Transfer and Finance clearance are complete, then approve
          to advance it to the DDFC Admin for possession letter signing.
        </p>

        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">
            Comments <span className="text-xs text-gray-400 font-normal">(required to reject)</span>
          </label>
          <textarea
            rows={3}
            value={comments}
            onChange={(e) => setComments(e.target.value)}
            placeholder="Notes or reason for rejection..."
            className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 resize-none"
          />
        </div>

        <div className="flex gap-2 pt-1">
          <Button
            variant="primary"
            className="flex-1"
            loading={submitting && actionMode === 'approve'}
            onClick={handleApprove}
            icon={<CheckCircle size={16} />}
          >
            Approve
          </Button>
          <Button
            variant="danger"
            className="flex-1"
            loading={submitting && actionMode === 'reject'}
            onClick={handleReject}
            icon={<XCircle size={16} />}
          >
            Reject
          </Button>
        </div>
      </div>
    </Card>
  );
};
