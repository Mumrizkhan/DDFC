import React, { useMemo, useState } from 'react';
import { CheckCircle, FileText, Maximize2, Minimize2, XCircle } from 'lucide-react';
import * as Dialog from '@radix-ui/react-dialog';
import { Card } from '../../../components/ui/Card';
import { Button } from '../../../components/ui/Button';
import { Textarea } from '../../../components/ui/Input';
import type { PossessionRequest, RequestDocument } from '../../../types';
import { useAppDispatch } from '../../../store/hooks';
import { verifyDocuments } from '../../../store/slices/requestsSlice';
import { toast } from 'react-toastify';

interface Props {
  request: PossessionRequest;
  requestId: string;
}

type DocumentState = 'complete' | 'incomplete' | undefined;

export const DocumentVerificationPanel: React.FC<Props> = ({ request, requestId }) => {
  const dispatch = useAppDispatch();
  const [states, setStates] = useState<Record<string, DocumentState>>({});
  const [comments, setComments] = useState('');
  const [submitting, setSubmitting] = useState(false);

  const documents = useMemo(() => {
    const legacy: RequestDocument[] = [
      request.allotmentLetterUrl ? { documentId: 'legacy-allotment', documentType: 'Allotment Letter', fileUrl: request.allotmentLetterUrl, uploadedAt: request.submittedAt } : null,
      request.cnicUrl ? { documentId: 'legacy-cnic', documentType: 'CNIC', fileUrl: request.cnicUrl, uploadedAt: request.submittedAt } : null,
      request.messageScreenshotUrl ? { documentId: 'legacy-message', documentType: 'Message Screenshot', fileUrl: request.messageScreenshotUrl, uploadedAt: request.submittedAt } : null,
      request.eStampPaperUrl ? { documentId: 'legacy-stamp', documentType: 'E-Stamp Paper', fileUrl: request.eStampPaperUrl, uploadedAt: request.submittedAt } : null,
      request.authorizedPersonCnicUrl ? { documentId: 'legacy-authorized-cnic', documentType: 'Authorized Person CNIC', fileUrl: request.authorizedPersonCnicUrl, uploadedAt: request.submittedAt } : null,
    ].filter(Boolean) as RequestDocument[];
    const seenUrls = new Set<string>();
    return [...(request.documents ?? []), ...legacy].filter((document) => {
      if (!document.fileUrl || seenUrls.has(document.fileUrl)) return false;
      seenUrls.add(document.fileUrl);
      return true;
    });
  }, [request]);

  const allComplete = documents.length > 0 && documents.every((doc) => states[doc.documentId] === 'complete');

  const setDocumentState = (documentId: string, state: DocumentState) => {
    setStates((current) => ({ ...current, [documentId]: state }));
  };

  const submit = async (action: 'Approve' | 'Incomplete') => {
    if (action === 'Approve' && !allComplete) {
      toast.error('Mark every document as complete or incomplete before approving');
      return;
    }
    if (action === 'Incomplete' && !comments.trim()) {
      toast.error('Add comments describing the incomplete document');
      return;
    }

    setSubmitting(true);
    try {
      await dispatch(verifyDocuments({
        id: requestId,
        action,
        comments: comments.trim() || undefined,
      })).unwrap();
      toast.success(action === 'Approve' ? 'Documents verified — sent to Transfer Branch' : 'Documents marked incomplete');
      setComments('');
    } catch {
      toast.error('Document verification failed');
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <Card title="Documents Verification">
      <div className="space-y-5">
        <p className="text-sm text-gray-500">
          Review every submitted document and mark it complete or incomplete. Approve only after all documents have been checked.
        </p>

        {documents.length === 0 ? (
          <p className="rounded-lg border border-dashed border-gray-300 bg-gray-50 p-4 text-sm text-gray-500">
            No documents uploaded for this request.
          </p>
        ) : (
          <div className="space-y-2">
            {documents.map((doc) => {
              const state = states[doc.documentId];
              return (
                <div key={doc.documentId} className="flex flex-wrap items-center gap-3 rounded-lg border border-gray-200 bg-gray-50 p-3">
                  <FileText size={16} className="shrink-0 text-gray-500" />
                  <div className="min-w-0 flex-1">
                    <p className="truncate text-sm font-medium text-gray-800">{doc.documentType}</p>
                  </div>
                  <Button
                    variant={state === 'complete' ? 'primary' : 'secondary'}
                    size="sm"
                    onClick={() => setDocumentState(doc.documentId, 'complete')}
                    icon={<CheckCircle size={14} />}
                  >
                    Complete
                  </Button>
                  <Button
                    variant={state === 'incomplete' ? 'danger' : 'secondary'}
                    size="sm"
                    onClick={() => setDocumentState(doc.documentId, 'incomplete')}
                    icon={<XCircle size={14} />}
                  >
                    Incomplete
                  </Button>
                  <Dialog.Root>
                    <div className="relative h-64 w-full overflow-hidden rounded-lg border border-gray-200 bg-white">
                      <object data={doc.fileUrl} aria-label={`${doc.documentType} preview`} className="h-full w-full">
                        <div className="flex h-full items-center justify-center p-4 text-sm text-gray-500">
                          <a href={doc.fileUrl} target="_blank" rel="noreferrer" className="text-blue-600 hover:underline">
                            Open {doc.documentType}
                          </a>
                        </div>
                      </object>
                      <Dialog.Trigger asChild>
                        <button
                          type="button"
                          title={`Maximize ${doc.documentType}`}
                          aria-label={`Maximize ${doc.documentType}`}
                          className="absolute right-2 top-2 flex h-9 w-9 items-center justify-center rounded-lg border border-gray-300 bg-white text-gray-700 shadow-sm hover:bg-gray-100 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-blue-500"
                        >
                          <Maximize2 size={18} />
                        </button>
                      </Dialog.Trigger>
                    </div>
                    <Dialog.Portal>
                      <Dialog.Overlay className="fixed inset-0 z-50 bg-black/60" />
                      <Dialog.Content aria-describedby={undefined} className="fixed inset-2 z-50 flex flex-col overflow-hidden rounded-lg bg-white shadow-xl sm:inset-4">
                        <div className="flex shrink-0 items-center justify-between gap-3 border-b border-gray-200 px-4 py-3">
                          <Dialog.Title className="min-w-0 break-words text-base font-semibold text-gray-900">
                            {doc.documentType}
                          </Dialog.Title>
                          <Dialog.Close asChild>
                            <button
                              type="button"
                              title="Minimize document"
                              aria-label="Minimize document"
                              className="flex h-9 w-9 shrink-0 items-center justify-center rounded-lg text-gray-700 hover:bg-gray-100 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-blue-500"
                            >
                              <Minimize2 size={18} />
                            </button>
                          </Dialog.Close>
                        </div>
                        <object data={doc.fileUrl} aria-label={doc.documentType} className="min-h-0 w-full flex-1">
                          <div className="flex h-full items-center justify-center p-4 text-sm text-gray-500">
                            <a href={doc.fileUrl} target="_blank" rel="noreferrer" className="text-blue-600 hover:underline">
                              Open {doc.documentType}
                            </a>
                          </div>
                        </object>
                      </Dialog.Content>
                    </Dialog.Portal>
                  </Dialog.Root>
                </div>
              );
            })}
          </div>
        )}

        <Textarea
          label="Comments"
          placeholder="Add notes, especially when marking documents incomplete"
          value={comments}
          onChange={(event) => setComments(event.target.value)}
          rows={3}
        />

        <div className="flex gap-2">
          <Button
            variant="primary"
            className="flex-1"
            disabled={!allComplete}
            loading={submitting}
            onClick={() => submit('Approve')}
            icon={<CheckCircle size={16} />}
          >
            Verify &amp; Send to Transfer
          </Button>
          <Button
            variant="danger"
            className="flex-1"
            loading={submitting}
            onClick={() => submit('Incomplete')}
            icon={<XCircle size={16} />}
          >
            Mark Incomplete
          </Button>
        </div>
      </div>
    </Card>
  );
};
