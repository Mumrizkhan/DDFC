import React, { useState } from 'react';
import { UploadCloud, History, FileSignature } from 'lucide-react';
import { Card } from '../../../components/ui/Card';
import { Button } from '../../../components/ui/Button';
import { Input } from '../../../components/ui/Input';
import { Badge } from '../../../components/ui/Badge';
import { FileUploadButton } from '../../../components/ui/FileUploadButton';
import { UndertakingModal } from '../../../components/shared/UndertakingModal';
import { CadSection } from '../../../components/shared/CadSection';
import type { PossessionRequest } from '../../../types';
import { format } from 'date-fns';
import { useAppDispatch } from '../../../store/hooks';
import { uploadPlan } from '../../../store/slices/requestsSlice';
import { toast } from 'react-toastify';

interface Props {
  request: PossessionRequest;
  requestId: string;
}

export const ArchitecturePanel: React.FC<Props> = ({ request, requestId }) => {
  const dispatch = useAppDispatch();
  const [fileUrl, setFileUrl]           = useState('');
  const [notes, setNotes]               = useState('');
  const [uploading, setUploading]       = useState(false);
  const [showUndertaking, setShowUndertaking] = useState(false);

  const plans = (request.documents ?? []).filter((d) =>
    d.documentType.toLowerCase().includes('plan')
  );

  const handleUpload = async () => {
    if (!fileUrl.trim()) return;
    setUploading(true);
    try {
      await dispatch(uploadPlan({ id: requestId, data: { fileUrl: fileUrl.trim(), notes: notes.trim() || undefined } })).unwrap();
      toast.success('Plan uploaded successfully');
      setFileUrl('');
      setNotes('');
    } catch {
      toast.error('Failed to upload plan');
    } finally {
      setUploading(false);
    }
  };

  return (
    <Card title="Architect Panel">
      <div className="space-y-6">

        {/* Undertaking button */}
        <div className="flex justify-end">
          <Button
            variant="outline"
            size="sm"
            icon={<FileSignature size={14} />}
            onClick={() => setShowUndertaking(true)}
          >
            Undertaking
          </Button>
        </div>
        {/* Customer Revision Requests */}
        {request.revisionHistory && request.revisionHistory.length > 0 && (
          <div>
            <h3 className="text-sm font-semibold text-gray-700 mb-2 flex items-center gap-2">
              <History size={14} />
              Customer Revision Requests
            </h3>
            <div className="space-y-2">
              {request.revisionHistory.map((rev, i) => (
                <div key={i} className="bg-amber-50 border border-amber-200 rounded-lg p-3 text-sm">
                  <p className="text-amber-800">{rev.note}</p>
                  <p className="text-xs text-amber-500 mt-1">
                    {format(new Date(rev.requestedAt), 'dd MMM yyyy HH:mm')}
                  </p>
                </div>
              ))}
            </div>
          </div>
        )}

        {/* Plan Upload */}
        <div>
          <h3 className="text-sm font-semibold text-gray-700 mb-3 flex items-center gap-2">
            <UploadCloud size={14} />
            Upload Architectural Plan
          </h3>
          <div className="space-y-3">
            {/* File picker */}
            <FileUploadButton
              label="Plan File"
              required
              value={fileUrl}
              onUploaded={(url) => setFileUrl(url)}
            />

            <Input
              label="Notes (optional)"
              placeholder="e.g. Initial draft per customer brief"
              value={notes}
              onChange={(e) => setNotes(e.target.value)}
            />
            <Button
              variant="primary"
              loading={uploading}
              disabled={!fileUrl}
              onClick={handleUpload}
              icon={<UploadCloud size={14} />}
            >
              Submit Plan
            </Button>
          </div>
        </div>

        {/* Plan Version History */}
        {plans.length > 0 && (
          <div>
            <h3 className="text-sm font-semibold text-gray-700 mb-2">Plan Versions</h3>
            <div className="space-y-2">
              {plans.map((doc, i) => (
                <div key={doc.documentId} className="flex items-center justify-between p-3 bg-gray-50 rounded-lg text-sm">
                  <div>
                    <span className="font-medium">Version {plans.length - i}</span>
                    <span className="text-gray-400 ml-2">
                      {format(new Date(doc.uploadedAt), 'dd MMM yyyy')}
                    </span>
                  </div>
                  <div className="flex items-center gap-2">
                    {i === 0 && <Badge variant="success" size="sm">Latest</Badge>}
                    <a href={doc.fileUrl} target="_blank" rel="noopener noreferrer" className="text-blue-600 hover:underline">
                      View
                    </a>
                  </div>
                </div>
              ))}
            </div>
          </div>
        )}
      </div>

        {/* Architecture CAD */}
        <CadSection
          cadType="Architecture"
          label="Architecture CAD"
          requestId={requestId}
          request={request}
          accentClass="border-blue-200 bg-blue-50"
        />

      <UndertakingModal
        isOpen={showUndertaking}
        onClose={() => setShowUndertaking(false)}
        request={request}
      />
    </Card>
  );
};
