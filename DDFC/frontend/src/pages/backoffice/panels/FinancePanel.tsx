import React, { useState } from 'react';
import { CheckCircle, DollarSign, XCircle } from 'lucide-react';
import { Card } from '../../../components/ui/Card';
import { Input } from '../../../components/ui/Input';
import { Button } from '../../../components/ui/Button';
import { FileUploadButton } from '../../../components/ui/FileUploadButton';
import type { PossessionRequest } from '../../../types';
import { useAppDispatch } from '../../../store/hooks';
import { financeApprove, financeReject, confirmPayment, attachDocument } from '../../../store/slices/requestsSlice';
import { toast } from 'react-toastify';

interface Props {
  request: PossessionRequest;
  requestId: string;
}

export const FinancePanel: React.FC<Props> = ({ request, requestId }) => {
  const dispatch = useAppDispatch();
  const [amountPaid, setAmountPaid] = useState('');
  const [challanNo, setChallanNo] = useState('');
  const [adcAmount, setAdcAmount] = useState('');
  const [approveComments, setApproveComments] = useState('');
  const [showRejectForm, setShowRejectForm] = useState(false);
  const [rejectComments, setRejectComments] = useState('');
  const [submittingApprove, setSubmittingApprove] = useState(false);
  const [submittingReject, setSubmittingReject] = useState(false);
  const [submittingPayment, setSubmittingPayment] = useState(false);

  // Plot Finance Statement document
  const [financeStatementUrls, setFinanceStatementUrls] = useState<string[]>([]);
  const [submittingDoc, setSubmittingDoc] = useState(false);

  const handleFinanceApprove = async () => {
    setSubmittingApprove(true);
    try {
      await dispatch(
        financeApprove({
          id: requestId,
          comments: approveComments || undefined,
          adcAmount: adcAmount ? parseFloat(adcAmount) : undefined,
        })
      ).unwrap();
      toast.success('Finance clearance approved');
    } catch {
      toast.error('Failed to approve finance clearance');
    } finally {
      setSubmittingApprove(false);
    }
  };

  const handleFinanceReject = async () => {
    if (!rejectComments.trim()) {
      toast.error('Please provide a reason for rejection');
      return;
    }
    setSubmittingReject(true);
    try {
      await dispatch(financeReject({ id: requestId, comments: rejectComments.trim() })).unwrap();
      toast.success('Finance step rejected');
    } catch {
      toast.error('Failed to reject finance step');
    } finally {
      setSubmittingReject(false);
    }
  };

  const handleAttachFinanceStatement = async (url: string) => {
    setSubmittingDoc(true);
    try {
      await dispatch(attachDocument({ id: requestId, documentType: 'PlotFinanceStatement', fileUrl: url })).unwrap();
      setFinanceStatementUrls((prev) => [...prev, url]);
      toast.success('Plot Finance Statement attached');
    } catch {
      toast.error('Failed to attach document');
    } finally {
      setSubmittingDoc(false);
    }
  };

  const handleConfirmPayment = async () => {    if (!amountPaid || !challanNo.trim()) return;
    setSubmittingPayment(true);
    try {
      await dispatch(
        confirmPayment({ id: requestId, data: { amountPaid: parseFloat(amountPaid), challanNo: challanNo.trim() } })
      ).unwrap();
      toast.success('Payment confirmed');
      setAmountPaid('');
      setChallanNo('');
    } catch {
      toast.error('Failed to confirm payment');
    } finally {
      setSubmittingPayment(false);
    }
  };

  const unpaidPayments = (request.payments ?? []).filter((p) => !p.isPaid);

  // Existing Plot Finance Statement docs on this request
  const existingFinanceDocs = (request.documents ?? []).filter(
    (d) => d.documentType === 'Plot Finance Statement'
  );
  const totalFinanceDocs = existingFinanceDocs.length + financeStatementUrls.length;

  return (
    <Card title="Finance Branch Panel">
      <div className="space-y-6">
        {/* Existing payments */}
        {request.payments && request.payments.length > 0 && (
          <div>
            <h3 className="text-sm font-semibold text-gray-700 mb-2 flex items-center gap-2">
              <DollarSign size={14} />
              Challans / Payments
            </h3>
            <div className="space-y-2">
              {request.payments.map((p) => (
                <div
                  key={p.paymentId}
                  className="flex items-center justify-between p-3 bg-gray-50 rounded-lg text-sm"
                >
                  <div>
                    <span className="font-medium">Challan #{p.challanNumber}</span>
                    <span className="text-gray-400 ml-2">PKR {p.amount.toLocaleString()}</span>
                  </div>
                  <span
                    className={`text-xs font-medium ${p.isPaid ? 'text-green-600' : 'text-amber-600'}`}
                  >
                    {p.isPaid ? 'Paid' : 'Pending'}
                  </span>
                </div>
              ))}
            </div>
          </div>
        )}

        {/* Show existing ADC if already set */}
        {request.adcAmount !== undefined && request.adcAmount !== null && (
          <div className="p-3 bg-amber-50 border border-amber-200 rounded-lg text-sm text-amber-800">
            <span className="font-semibold">Additional Development Charges (ADC):</span>{' '}
            PKR {request.adcAmount.toLocaleString()}
          </div>
        )}

        {/* Confirm a payment */}
        {unpaidPayments.length > 0 && (
          <div>
            <h3 className="text-sm font-semibold text-gray-700 mb-3">Confirm Payment Receipt</h3>
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
              <Input
                label="Amount Paid (PKR)"
                type="number"
                placeholder="0"
                value={amountPaid}
                onChange={(e) => setAmountPaid(e.target.value)}
              />
              <Input
                label="Challan / Receipt No"
                placeholder="e.g. CH-2026-001"
                value={challanNo}
                onChange={(e) => setChallanNo(e.target.value)}
              />
            </div>
            <Button
              variant="secondary"
              className="mt-3"
              disabled={!amountPaid || !challanNo.trim()}
              loading={submittingPayment}
              onClick={handleConfirmPayment}
              icon={<DollarSign size={16} />}
            >
              Confirm Payment
            </Button>
          </div>
        )}

        {/* Plot Finance Statement */}
        <div className="border rounded-xl p-4 space-y-3 bg-amber-50 border-amber-200">
          <h3 className="text-sm font-semibold text-amber-800">
            Plot Finance Statement <span className="text-red-500">*</span>
            <span className="text-xs text-amber-600 font-normal ml-2">({totalFinanceDocs}/2 uploaded)</span>
          </h3>
          {existingFinanceDocs.length > 0 && (
            <div className="space-y-1">
              {existingFinanceDocs.map((d) => (
                <div key={d.documentId} className="flex items-center gap-1 text-xs bg-white border rounded px-2 py-1">
                  <span className="text-green-600">✓</span>
                  <a href={d.fileUrl} target="_blank" rel="noreferrer" className="truncate max-w-[200px] underline text-blue-600">
                    {d.fileUrl.split('/').pop()}
                  </a>
                </div>
              ))}
            </div>
          )}
          {totalFinanceDocs < 2 && (
            <FileUploadButton
              label="Upload Plot Finance Statement"
              required={totalFinanceDocs === 0}
              onUploaded={handleAttachFinanceStatement}
            />
          )}
          {submittingDoc && <p className="text-xs text-amber-600">Attaching document…</p>}
        </div>

        {/* Finance approval */}
        <div>
          <h3 className="text-sm font-semibold text-gray-700 mb-3">Finance Clearance</h3>
          <div className="space-y-3">
            <Input
              label="Additional Development Charges / ADC (PKR) — optional"
              type="number"
              placeholder="0"
              value={adcAmount}
              onChange={(e) => setAdcAmount(e.target.value)}
            />
            <Input
              label="Approval Comments (optional)"
              placeholder="Any notes for this clearance..."
              value={approveComments}
              onChange={(e) => setApproveComments(e.target.value)}
            />
          </div>
          <p className="text-xs text-gray-400 mt-2 mb-3">
            Confirm all dues are settled and accounts are cleared before approving.
          </p>
          <Button
            variant="primary"
            className="w-full"
            loading={submittingApprove}
            onClick={handleFinanceApprove}
            icon={<CheckCircle size={16} />}
          >
            Approve Finance Clearance
          </Button>
        </div>

        {/* Finance rejection */}
        <div className="border-t pt-4">
          {!showRejectForm ? (
            <Button
              variant="danger"
              className="w-full"
              onClick={() => setShowRejectForm(true)}
              icon={<XCircle size={16} />}
            >
              Reject Finance Step
            </Button>
          ) : (
            <div className="space-y-3">
              <h3 className="text-sm font-semibold text-red-700">Reject Finance Step</h3>
              <textarea
                className="w-full border border-red-300 rounded-lg p-3 text-sm focus:outline-none focus:ring-2 focus:ring-red-300"
                rows={3}
                placeholder="Reason for rejection (required)..."
                value={rejectComments}
                onChange={(e) => setRejectComments(e.target.value)}
              />
              <div className="flex gap-2">
                <Button
                  variant="danger"
                  loading={submittingReject}
                  onClick={handleFinanceReject}
                  icon={<XCircle size={16} />}
                >
                  Confirm Rejection
                </Button>
                <Button
                  variant="secondary"
                  onClick={() => { setShowRejectForm(false); setRejectComments(''); }}
                >
                  Cancel
                </Button>
              </div>
            </div>
          )}
        </div>
      </div>
    </Card>
  );
};
