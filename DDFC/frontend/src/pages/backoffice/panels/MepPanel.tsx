import React, { useState } from 'react';
import { CheckCircle } from 'lucide-react';
import { Card } from '../../../components/ui/Card';
import { Button } from '../../../components/ui/Button';
import { Textarea } from '../../../components/ui/Input';
import { FileUploadButton } from '../../../components/ui/FileUploadButton';
import { CadSection } from '../../../components/shared/CadSection';
import type { PossessionRequest } from '../../../types';
import { useAppDispatch } from '../../../store/hooks';
import { mepComplete } from '../../../store/slices/requestsSlice';
import { toast } from 'react-toastify';

interface Props {
  requestId: string;
  request: PossessionRequest;
}

export const MepPanel: React.FC<Props> = ({ requestId, request }) => {
  const dispatch = useAppDispatch();
  const [fileUrl, setFileUrl] = useState('');
  const [observations, setObservations] = useState('');
  const [loading, setLoading] = useState(false);

  const handleComplete = async () => {
    if (!fileUrl.trim()) return;
    setLoading(true);
    try {
      await dispatch(
        mepComplete({ id: requestId, data: { fileUrl: fileUrl.trim(), observations: observations.trim() || undefined } })
      ).unwrap();
      toast.success('MEP review marked complete');
      setFileUrl('');
      setObservations('');
    } catch {
      toast.error('Failed to submit MEP review');
    } finally {
      setLoading(false);
    }
  };

  return (
    <Card title="MEP Engineering Panel">
      <div className="space-y-4">
        <Textarea
          label="MEP Observations"
          placeholder="Enter mechanical, electrical and plumbing findings, compliance checks..."
          value={observations}
          onChange={(e) => setObservations(e.target.value)}
          rows={5}
        />
        <FileUploadButton
          label="MEP Report"
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
          Submit MEP Review
        </Button>

        {/* MEP CAD */}
        <CadSection
          cadType="MEP"
          label="MEP CAD"
          requestId={requestId}
          request={request}
          accentClass="border-violet-200 bg-violet-50"
        />
      </div>
    </Card>
  );
};
