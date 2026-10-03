import React, { useState } from 'react';
import { CheckCircle, FileText, Printer } from 'lucide-react';
import { Card } from '../../../components/ui/Card';
import { Button } from '../../../components/ui/Button';
import type { PossessionRequest } from '../../../types';
import { useAppDispatch, useAppSelector } from '../../../store/hooks';
import { completeBuildingControl } from '../../../store/slices/requestsSlice';
import { viewWatermarkedFile } from '../../../services/documentViewer';
import { toast } from 'react-toastify';

interface Props {
  request: PossessionRequest;
  requestId: string;
}

export const BuildingControlPanel: React.FC<Props> = ({ request, requestId }) => {
  const dispatch = useAppDispatch();
  const staffUser = useAppSelector((state) => state.auth.staffUser);
  const [submitting, setSubmitting] = useState(false);
  const [openingUrl, setOpeningUrl] = useState<string | null>(null);
  const finalDocuments = (request.documents ?? []).filter((document) =>
    document.fileUrl && /architectural|structural|mep|3d|cad|draft/i.test(document.documentType) &&
    !/revision markup/i.test(document.documentType));

  const handlePrint = async (url: string) => {
    setOpeningUrl(url);
    try {
      await viewWatermarkedFile(url, staffUser?.fullName ?? '');
    } catch (error) {
      toast.error(error instanceof Error ? error.message : 'Failed to open final document');
    } finally {
      setOpeningUrl(null);
    }
  };

  const handleSubmit = async () => {
    setSubmitting(true);
    try {
      await dispatch(completeBuildingControl(requestId)).unwrap();
      toast.success('Final documents cleared — sent to DHA Design Head for final approval');
    } catch (error) {
      toast.error(typeof error === 'string' ? error : 'Failed to submit Building Control clearance');
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <Card title="Building Control — Final Documents">
      <div className="space-y-4">
        {finalDocuments.length > 0 ? (
          <ul className="divide-y divide-gray-200">
            {finalDocuments.map((document) => {
              const printable = /\.(pdf|png|jpe?g)(?:[?#]|$)/i.test(document.fileUrl);
              return (
                <li key={document.documentId} className="flex flex-wrap items-center justify-between gap-3 py-3">
                  <div className="flex min-w-0 flex-1 items-center gap-2">
                    <FileText size={16} className="shrink-0 text-gray-500" />
                    <span className="break-words text-sm font-medium text-gray-800">{document.documentType}</span>
                  </div>
                  <Button
                    variant="outline"
                    size="sm"
                    disabled={!printable || openingUrl !== null}
                    loading={openingUrl === document.fileUrl}
                    title={printable ? `View / Print ${document.documentType}` : 'PDF or image export required for printing'}
                    onClick={() => handlePrint(document.fileUrl)}
                    icon={<Printer size={14} />}
                  >
                    View / Print
                  </Button>
                </li>
              );
            })}
          </ul>
        ) : (
          <p className="py-4 text-center text-sm text-gray-500">No final documents available.</p>
        )}
        <Button
          variant="primary"
          className="w-full"
          disabled={finalDocuments.length === 0 || openingUrl !== null}
          loading={submitting}
          onClick={handleSubmit}
          icon={<CheckCircle size={16} />}
        >
          Complete Building Control &amp; Send to Final Approval
        </Button>
      </div>
    </Card>
  );
};
