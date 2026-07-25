import React, { useState } from 'react';
import { CheckCircle } from 'lucide-react';
import { Card } from '../../../components/ui/Card';
import { Button } from '../../../components/ui/Button';
import { Textarea } from '../../../components/ui/Input';
import { FileUploadButton } from '../../../components/ui/FileUploadButton';
import { CadSection } from '../../../components/shared/CadSection';
import type { PossessionRequest } from '../../../types';
import { useAppDispatch } from '../../../store/hooks';
import { structureComplete } from '../../../store/slices/requestsSlice';
import { toast } from 'react-toastify';

interface Props {
  requestId: string;
  request: PossessionRequest;
}

export const StructurePanel: React.FC<Props> = ({ requestId, request }) => {
  const dispatch = useAppDispatch();
  const [fileUrl, setFileUrl] = useState('');
  const [observations, setObservations] = useState('');
  const [loading, setLoading] = useState(false);

  const handleComplete = async () => {
    if (!fileUrl.trim()) return;
    setLoading(true);
    try {
      await dispatch(
        structureComplete({ id: requestId, data: { fileUrl: fileUrl.trim(), observations: observations.trim() || undefined } })
      ).unwrap();
      toast.success('Structural review marked complete');
      setFileUrl('');
      setObservations('');
    } catch {
      toast.error('Failed to submit structural review');
    } finally {
      setLoading(false);
    }
  };

  return (
    <Card title="Structure Engineering Panel">
      <div className="space-y-4">
        <Textarea
          label="Structural Observations"
          placeholder="Enter structural engineering findings, load analysis, foundation assessment..."
          value={observations}
          onChange={(e) => setObservations(e.target.value)}
          rows={5}
        />
        <FileUploadButton
          label="Structural Report"
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
          Submit Structural Review
        </Button>

        {/* Structure CAD */}
        <CadSection
          cadType="Structure"
          label="Structure CAD"
          requestId={requestId}
          request={request}
          accentClass="border-emerald-200 bg-emerald-50"
        />
      </div>
    </Card>
  );
};
