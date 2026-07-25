import React, { useState } from 'react';
import { CheckCircle, FileText } from 'lucide-react';
import { Card } from '../../../components/ui/Card';
import { Input } from '../../../components/ui/Input';
import { Button } from '../../../components/ui/Button';
import type { PossessionRequest } from '../../../types';
import { useAppDispatch } from '../../../store/hooks';
import { transferApprove, confirmPayment } from '../../../store/slices/requestsSlice';
import { toast } from 'react-toastify';

const NDC_ITEMS = [
  'Property Tax Clearance',
  'Society Dues Clearance',
  'Utility Bills Clearance',
  'Mortgage Release (if any)',
  'Encumbrance Certificate',
];

interface Props {
  request: PossessionRequest;
  requestId: string;
}

export const TransferFinancePanel: React.FC<Props> = ({ request, requestId }) => {
  const dispatch = useAppDispatch();
  const [duesAmount, setDuesAmount] = useState('');
  const [challanNo, setChallanNo] = useState('');
  const [ndcChecked, setNdcChecked] = useState<string[]>([]);
  const [litigationNote, setLitigationNote] = useState('');
  const [submitting, setSubmitting] = useState(false);

  const toggleNdc = (item: string) =>
    setNdcChecked((prev) =>
      prev.includes(item) ? prev.filter((x) => x !== item) : [...prev, item]
    );

  const isTransfer = request.status === 'Submitted' || request.status === 'TransferApproved';

  const handleTransferApprove = async () => {
    if (ndcChecked.length < NDC_ITEMS.length) return;
    setSubmitting(true);
    try {
      await dispatch(transferApprove({ id: requestId, comments: litigationNote.trim() || undefined })).unwrap();
      toast.success('Transfer approved');
    } catch {
      toast.error('Failed to approve transfer');
    } finally {
      setSubmitting(false);
    }
  };

  const handleConfirmPayment = async () => {
    if (!duesAmount || !challanNo.trim()) return;
    setSubmitting(true);
    try {
      await dispatch(confirmPayment({ id: requestId, data: { amountPaid: parseFloat(duesAmount), challanNo: challanNo.trim() } })).unwrap();
      toast.success('Payment confirmed');
      setDuesAmount(''); setChallanNo('');
    } catch {
      toast.error('Failed to confirm payment');
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <Card title={isTransfer ? 'Transfer Branch Panel' : 'Finance Branch Panel'}>
      <div className="space-y-4">
        {isTransfer ? (
          <>
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
            </div>

            {/* Litigation */}
            <div>
              <Input
                label="Litigation / Dispute Notes (if any)"
                placeholder="Leave blank if none"
                value={litigationNote}
                onChange={(e) => setLitigationNote(e.target.value)}
              />
            </div>

            <Button
              variant="primary"
              className="w-full"
              disabled={ndcChecked.length < NDC_ITEMS.length}
              loading={submitting}
              onClick={handleTransferApprove}
              icon={<CheckCircle size={16} />}
            >
              {ndcChecked.length < NDC_ITEMS.length
                ? `${NDC_ITEMS.length - ndcChecked.length} items pending`
                : 'Approve Transfer'}
            </Button>
          </>
        ) : (
          <>
            {/* Finance clearance */}
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <Input
                label="Dues Amount (PKR)"
                type="number"
                placeholder="0"
                value={duesAmount}
                onChange={(e) => setDuesAmount(e.target.value)}
              />
              <Input
                label="Challan / Receipt No"
                placeholder="e.g. CH-2024-001"
                value={challanNo}
                onChange={(e) => setChallanNo(e.target.value)}
              />
            </div>

            <Button
              variant="primary"
              className="w-full"
              disabled={!challanNo.trim() || !duesAmount}
              loading={submitting}
              onClick={handleConfirmPayment}
              icon={<CheckCircle size={16} />}
            >
              Confirm Finance Clearance
            </Button>
          </>
        )}
      </div>
    </Card>
  );
};
