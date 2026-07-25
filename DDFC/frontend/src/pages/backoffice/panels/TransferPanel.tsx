import React, { useState } from 'react';
import { CheckCircle, FileText, XCircle, MessageSquare } from 'lucide-react';
import { Card } from '../../../components/ui/Card';
import { Input } from '../../../components/ui/Input';
import { Button } from '../../../components/ui/Button';
import { useAppDispatch } from '../../../store/hooks';
import { transferApprove, transferReject, transferClarification } from '../../../store/slices/requestsSlice';
import { toast } from 'react-toastify';

const NDC_ITEMS = [
  'Property Tax Clearance',
  'Society Dues Clearance',
  'Utility Bills Clearance',
  'Mortgage Release (if any)',
  'Encumbrance Certificate',
];

type ActionMode = 'approve' | 'reject' | 'clarification' | null;

interface Props {
  requestId: string;
}

export const TransferPanel: React.FC<Props> = ({ requestId }) => {
  const dispatch = useAppDispatch();
  const [ndcChecked, setNdcChecked] = useState<string[]>([]);
  const [litigationNote, setLitigationNote] = useState('');
  const [actionMode, setActionMode] = useState<ActionMode>(null);
  const [comments, setComments] = useState('');
  const [submitting, setSubmitting] = useState(false);

  const toggleNdc = (item: string) =>
    setNdcChecked((prev) =>
      prev.includes(item) ? prev.filter((x) => x !== item) : [...prev, item]
    );

  const handleSubmit = async () => {
    if (!actionMode) return;
    if ((actionMode === 'reject' || actionMode === 'clarification') && !comments.trim()) {
      toast.error('Comments are required');
      return;
    }
    setSubmitting(true);
    try {
      if (actionMode === 'approve') {
        await dispatch(transferApprove({ id: requestId, comments: litigationNote.trim() || undefined })).unwrap();
        toast.success('Transfer approved');
      } else if (actionMode === 'reject') {
        await dispatch(transferReject({ id: requestId, comments: comments.trim() })).unwrap();
        toast.success('Transfer rejected');
      } else {
        await dispatch(transferClarification({ id: requestId, comments: comments.trim() })).unwrap();
        toast.success('Clarification requested — sent back to Reception');
      }
      setActionMode(null);
      setComments('');
    } catch {
      toast.error('Action failed');
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <Card title="Transfer Branch Panel">
      <div className="space-y-4">
        {/* NDC Checklist */}
        <div>
          <h3 className="text-sm font-semibold text-gray-700 mb-2 flex items-center gap-2">
            <FileText size={14} />
            No-Dues / Clearance Checklist
          </h3>
          <div className="space-y-2">
            {NDC_ITEMS.map((item) => (
              <label key={item} className="flex items-center gap-2 text-sm cursor-pointer">
                <input
                  type="checkbox"
                  checked={ndcChecked.includes(item)}
                  onChange={() => toggleNdc(item)}
                  className="rounded text-emerald-600"
                />
                <span>{item}</span>
              </label>
            ))}
          </div>
          {ndcChecked.length > 0 && ndcChecked.length < NDC_ITEMS.length && (
            <p className="mt-2 text-xs text-amber-600">
              {NDC_ITEMS.length - ndcChecked.length} item(s) still pending
            </p>
          )}
        </div>

        {/* Litigation / dispute notes */}
        <div>
          <Input
            label="Litigation / Dispute Notes (optional)"
            placeholder="Leave blank if no disputes"
            value={litigationNote}
            onChange={(e) => setLitigationNote(e.target.value)}
          />
        </div>

        {/* Comment field — shown when reject or clarification is selected */}
        {(actionMode === 'reject' || actionMode === 'clarification') && (
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              {actionMode === 'reject' ? 'Rejection Reason' : 'Clarification Required'}{' '}
              <span className="text-red-500">*</span>
            </label>
            <textarea
              rows={3}
              value={comments}
              onChange={(e) => setComments(e.target.value)}
              placeholder={
                actionMode === 'reject'
                  ? 'Explain why the transfer is being rejected...'
                  : 'Describe the information needed from Reception...'
              }
              className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 resize-none"
            />
          </div>
        )}

        {/* Action buttons */}
        <div className="flex gap-2 pt-1">
          <Button
            variant="primary"
            className="flex-1"
            disabled={ndcChecked.length < NDC_ITEMS.length}
            loading={submitting && actionMode === 'approve'}
            onClick={() => { setActionMode('approve'); setComments(''); handleSubmit(); }}
            icon={<CheckCircle size={16} />}
          >
            {ndcChecked.length < NDC_ITEMS.length
              ? `${NDC_ITEMS.length - ndcChecked.length} pending`
              : 'Approve'}
          </Button>

          <Button
            variant="secondary"
            className="flex-1"
            loading={submitting && actionMode === 'clarification'}
            onClick={() =>
              actionMode === 'clarification' ? handleSubmit() : setActionMode('clarification')
            }
            icon={<MessageSquare size={16} />}
          >
            {actionMode === 'clarification' ? 'Send Request' : 'Request Clarification'}
          </Button>

          <Button
            variant="danger"
            className="flex-1"
            loading={submitting && actionMode === 'reject'}
            onClick={() =>
              actionMode === 'reject' ? handleSubmit() : setActionMode('reject')
            }
            icon={<XCircle size={16} />}
          >
            {actionMode === 'reject' ? 'Confirm Reject' : 'Reject'}
          </Button>
        </div>

        {/* Cancel secondary action */}
        {actionMode && actionMode !== 'approve' && (
          <button
            onClick={() => { setActionMode(null); setComments(''); }}
            className="text-xs text-gray-400 hover:text-gray-600 underline"
          >
            Cancel
          </button>
        )}
      </div>
    </Card>
  );
};

