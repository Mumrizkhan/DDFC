import React from 'react';
import { CheckCircle } from 'lucide-react';
import { Card } from '../../../components/ui/Card';
import { Button } from '../../../components/ui/Button';
import { Textarea } from '../../../components/ui/Input';
import { FileUploadButton } from '../../../components/ui/FileUploadButton';
import type { PossessionRequest } from '../../../types';
import { useAppDispatch, useAppSelector } from '../../../store/hooks';
import { structureComplete, mepComplete } from '../../../store/slices/requestsSlice';
import { toast } from 'react-toastify';

interface Props {
  request: PossessionRequest;
  requestId: string;
}

export const StructureMepPanel: React.FC<Props> = ({ request: _request, requestId }) => {
  const dispatch = useAppDispatch();
  const dept = useAppSelector((s) => s.auth.staffUser?.departmentName ?? '');
  const [observations, setObservations] = React.useState('');
  const [fileUrl, setFileUrl] = React.useState('');
  const [loading, setLoading] = React.useState(false);

  const isMep = dept.toLowerCase().includes('mep');

  const handleComplete = async () => {
    if (!fileUrl.trim()) return;
    setLoading(true);
    try {
      const action = isMep ? mepComplete : structureComplete;
      await dispatch(action({ id: requestId, data: { fileUrl: fileUrl.trim(), observations: observations.trim() || undefined } })).unwrap();
      toast.success('Review marked complete');
      setFileUrl('');
      setObservations('');
    } catch {
      toast.error('Failed to submit review');
    } finally {
      setLoading(false);
    }
  };

  return (
    <Card title="Structure / MEP Panel">
      <div className="space-y-4">
        <div>
          <Textarea
            label="Technical Observations"
            placeholder="Enter structural / MEP observations and findings..."
            value={observations}
            onChange={(e) => setObservations(e.target.value)}
            rows={5}
          />
        </div>
        <FileUploadButton
          label="Report File"
          required
          value={fileUrl}
          onUploaded={(url) => setFileUrl(url)}
        />
        <Button
          variant="primary"
          className="w-full"
          disabled={!fileUrl}
          loading={loading}
          onClick={handleComplete}
          icon={<CheckCircle size={16} />}
        >
          Mark Review Complete
        </Button>
      </div>
    </Card>
  );
};
