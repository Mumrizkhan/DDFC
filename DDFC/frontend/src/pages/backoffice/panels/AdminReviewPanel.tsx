import React, { useState } from 'react';
import { CheckCircle, XCircle } from 'lucide-react';
import { Card } from '../../../components/ui/Card';
import { Button } from '../../../components/ui/Button';
import { Textarea } from '../../../components/ui/Input';
import type { PossessionRequest } from '../../../types';
import { useAppDispatch } from '../../../store/hooks';
import { adminReview } from '../../../store/slices/requestsSlice';
import { toast } from 'react-toastify';

interface Props {
  request: PossessionRequest;
  requestId: string;
}

export const AdminReviewPanel: React.FC<Props> = ({ request, requestId }) => {
  const dispatch = useAppDispatch();
  const [comments, setComments] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const [action, setAction] = useState<'Approve' | 'Reject' | null>(null);

  const submit = async (selectedAction: 'Approve' | 'Reject') => {
    if (selectedAction === 'Reject' && !comments.trim()) {
      toast.error('Please provide a reason for rejection');
      return;
    }

    setAction(selectedAction);
    setSubmitting(true);
    try {
      await dispatch(adminReview({
        id: requestId,
        data: {
          action: selectedAction,
          rejectionReason: comments.trim() || undefined,
        },
      })).unwrap();
      toast.success(selectedAction === 'Approve' ? 'Admin review approved' : 'Request rejected');
      setComments('');
    } catch {
      toast.error('Action failed');
    } finally {
      setSubmitting(false);
      setAction(null);
    }
  };

  return (
    <Card title="Admin – Post-Payment Review">
      <div className="space-y-4">
        <p className="text-sm text-gray-500">
          Review the paid request and approve it to continue to the Soil Test step, or reject it.
        </p>
        <Textarea
          label="Comments (required to reject)"
          placeholder="Notes or reason for rejection..."
          value={comments}
          onChange={(e) => setComments(e.target.value)}
          rows={3}
        />
        <div className="flex gap-2">
          <Button
            variant="primary"
            className="flex-1"
            loading={submitting && action === 'Approve'}
            onClick={() => submit('Approve')}
            icon={<CheckCircle size={16} />}
          >
            Approve
          </Button>
          <Button
            variant="danger"
            className="flex-1"
            loading={submitting && action === 'Reject'}
            onClick={() => submit('Reject')}
            icon={<XCircle size={16} />}
          >
            Reject
          </Button>
        </div>
      </div>
    </Card>
  );
};
