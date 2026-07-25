import React, { useState } from 'react';
import { GitMerge, CheckCircle, AlertCircle, ExternalLink } from 'lucide-react';
import { Modal } from '../ui/Modal';
import { Button } from '../ui/Button';
import { Input, Textarea } from '../ui/Input';
import { toast } from 'react-toastify';
import { useAppDispatch } from '../../store/hooks';
import { requestPlotMerge } from '../../store/slices/requestsSlice';
import type { PossessionRequest } from '../../types';

interface Props {
  isOpen: boolean;
  onClose: () => void;
  request: PossessionRequest;
}

export const PlotMergingModal: React.FC<Props> = ({ isOpen, onClose, request }) => {
  const dispatch = useAppDispatch();

  const [mergedPlotNumber, setMergedPlotNumber] = useState('');
  const [mergedPlotSector, setMergedPlotSector] = useState('');
  const [mergedPlotSize, setMergedPlotSize] = useState('');
  const [notes, setNotes] = useState('');
  const [submitting, setSubmitting] = useState(false);

  const existing = request.plotMerging;

  // Reset form when modal opens
  React.useEffect(() => {
    if (isOpen) {
      setMergedPlotNumber('');
      setMergedPlotSector('');
      setMergedPlotSize('');
      setNotes('');
    }
  }, [isOpen]);

  const handleSubmit = async () => {
    if (!mergedPlotNumber.trim()) {
      toast.error('Please enter the merged plot number');
      return;
    }
    if (!mergedPlotSector.trim()) {
      toast.error('Please enter the merged plot sector');
      return;
    }
    setSubmitting(true);
    try {
      await dispatch(
        requestPlotMerge({
          id: request.id,
          data: {
            mergedPlotNumber: mergedPlotNumber.trim(),
            mergedPlotSector: mergedPlotSector.trim(),
            mergedPlotSize: mergedPlotSize.trim() || undefined,
            notes: notes.trim() || undefined,
          },
        })
      ).unwrap();
      toast.success('Plot merge recorded — package fee will be doubled for the merged plot');
      onClose();
    } catch {
      toast.error('Failed to record plot merge');
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <Modal isOpen={isOpen} onClose={onClose} title="Plot Merging" size="md">
      <div className="space-y-5">

        {/* Info banner */}
        <div className="bg-violet-50 border border-violet-200 rounded-lg p-3 text-sm text-violet-800 space-y-1">
          <p className="font-semibold flex items-center gap-1.5">
            <GitMerge size={14} />
            What is Plot Merging?
          </p>
          <p className="text-violet-700 text-xs">
            An adjacent plot owned by the same owner is merged with this plot. The package fee
            will be <span className="font-semibold">doubled</span> to account for the merged plot area.
          </p>
        </div>

        {/* Fee doubling alert */}
        <div className="flex items-start gap-2 bg-amber-50 border border-amber-200 rounded-lg p-3 text-xs text-amber-800">
          <AlertCircle size={14} className="shrink-0 mt-0.5" />
          <span>
            The package fee for this request will be <strong>×2</strong> once the merge is confirmed.
            {request.packageTier && (
              <span className="block mt-0.5 text-amber-700">
                Current package: <strong>{request.packageTier}</strong>
                {request.totalFee != null && (
                  <span> — PKR {request.totalFee.toLocaleString()} → PKR {(request.totalFee * 2).toLocaleString()}</span>
                )}
              </span>
            )}
          </span>
        </div>

        {/* Already recorded */}
        {existing && (
          <div className="bg-green-50 border border-green-200 rounded-lg p-3 text-sm space-y-1">
            <div className="flex items-center gap-2 text-green-800 font-semibold">
              <CheckCircle size={14} />
              Plot merge already recorded
            </div>
            <p className="text-green-700">
              Merged plot: <span className="font-medium">{existing.mergedPlotNumber}</span>,
              Sector <span className="font-medium">{existing.mergedPlotSector}</span>
            </p>
            {existing.mergedPlotSize && (
              <p className="text-green-700 text-xs">Size: {existing.mergedPlotSize}</p>
            )}
            {existing.documentUrl && (
              <a href={existing.documentUrl} target="_blank" rel="noopener noreferrer"
                className="inline-flex items-center gap-1 text-green-700 underline text-xs">
                <ExternalLink size={11} /> View document
              </a>
            )}
          </div>
        )}

        {/* Form */}
        <div className="grid grid-cols-2 gap-3">
          <Input
            label="Merged Plot Number *"
            placeholder="e.g. 142-B"
            value={mergedPlotNumber}
            onChange={(e) => setMergedPlotNumber(e.target.value)}
          />
          <Input
            label="Merged Plot Sector *"
            placeholder="e.g. A-1"
            value={mergedPlotSector}
            onChange={(e) => setMergedPlotSector(e.target.value)}
          />
        </div>

        <Input
          label="Merged Plot Size"
          placeholder="e.g. 5 Marla, 1 Kanal"
          value={mergedPlotSize}
          onChange={(e) => setMergedPlotSize(e.target.value)}
          hint="Optional — for record keeping"
        />

        <Textarea
          label="Notes (optional)"
          placeholder="Any relevant details about the plot merge…"
          rows={2}
          value={notes}
          onChange={(e) => setNotes(e.target.value)}
        />

        <div className="flex gap-2 justify-end pt-1">
          <Button variant="ghost" onClick={onClose}>Cancel</Button>
          <Button
            variant="primary"
            loading={submitting}
            icon={<GitMerge size={14} />}
            onClick={handleSubmit}
          >
            {existing ? 'Update Merge' : 'Record Plot Merge'}
          </Button>
        </div>
      </div>
    </Modal>
  );
};
