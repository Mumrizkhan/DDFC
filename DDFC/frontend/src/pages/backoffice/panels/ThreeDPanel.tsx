import React, { useRef, useState } from 'react';
import { UploadCloud, FileCheck, Trash2, CheckCircle, Box } from 'lucide-react';
import { Card } from '../../../components/ui/Card';
import { Button } from '../../../components/ui/Button';
import type { PossessionRequest } from '../../../types';
import { useAppDispatch } from '../../../store/hooks';
import { threeDUploadFile, threeDComplete } from '../../../store/slices/requestsSlice';
import { CadSection } from '../../../components/shared/CadSection';
import { toast } from 'react-toastify';
import api from '../../../services/api';

const ACCEPTED_TYPES = '.skp,.jpeg,.jpg,.png';
const ACCEPTED_MIME  = ['application/octet-stream', 'image/jpeg', 'image/jpg', 'image/png'];

interface UploadedFile {
  fileName: string;
  fileUrl: string;
  fileType: string;
}

interface Props {
  request: PossessionRequest;
  requestId: string;
}

export const ThreeDPanel: React.FC<Props> = ({ request, requestId }) => {
  const dispatch    = useAppDispatch();
  const inputRef    = useRef<HTMLInputElement>(null);

  const [uploading, setUploading]   = useState(false);
  const [completing, setCompleting] = useState(false);
  const [queue, setQueue]           = useState<UploadedFile[]>([]);

  const existingFiles = request.threeDVisualizations ?? [];
  const isComplete    = request.status !== 'ArchitectureApproved';

  const handleFileChange = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const files = Array.from(e.target.files ?? []);
    if (!files.length) return;

    // Validate extensions
    const invalid = files.filter((f) => {
      const ext = f.name.split('.').pop()?.toLowerCase() ?? '';
      return !['skp', 'jpeg', 'jpg', 'png'].includes(ext);
    });
    if (invalid.length) {
      toast.error(`Unsupported file type(s): ${invalid.map((f) => f.name).join(', ')}. Use .skp, .jpeg or .png`);
      if (inputRef.current) inputRef.current.value = '';
      return;
    }

    setUploading(true);
    const uploaded: UploadedFile[] = [];
    for (const file of files) {
      try {
        const formData = new FormData();
        formData.append('file', file);
        const res = await api.post<{ url: string; originalName: string }>('/uploads', formData, {
          headers: { 'Content-Type': 'multipart/form-data' },
        });
        const ext = file.name.split('.').pop()?.toLowerCase() ?? 'file';
        uploaded.push({ fileName: res.data.originalName, fileUrl: res.data.url, fileType: ext });
        toast.success(`${file.name} uploaded`);
      } catch {
        toast.error(`Failed to upload ${file.name}`);
      }
    }
    setQueue((prev) => [...prev, ...uploaded]);
    if (inputRef.current) inputRef.current.value = '';
    setUploading(false);
  };

  const removeQueued = (idx: number) =>
    setQueue((prev) => prev.filter((_, i) => i !== idx));

  const handleSaveAll = async () => {
    if (!queue.length) return;
    setUploading(true);
    try {
      for (const f of queue) {
        await dispatch(threeDUploadFile({
          id: requestId,
          data: { fileUrl: f.fileUrl, fileName: f.fileName, fileType: f.fileType },
        })).unwrap();
      }
      setQueue([]);
      toast.success('3D files saved');
    } catch {
      toast.error('Failed to save files');
    } finally {
      setUploading(false);
    }
  };

  const handleComplete = async () => {
    if (existingFiles.length === 0 && queue.length === 0) {
      toast.error('Upload at least one 3D file before completing');
      return;
    }
    // Save any unsaved queue first
    if (queue.length) await handleSaveAll();
    setCompleting(true);
    try {
      await dispatch(threeDComplete(requestId)).unwrap();
      toast.success('3D Visualization step completed');
    } catch {
      toast.error('Failed to complete step');
    } finally {
      setCompleting(false);
    }
  };

  return (
    <Card title="3D Visualization">
      <div className="space-y-5">

        {/* Info banner */}
        <div className="p-3 bg-indigo-50 border border-indigo-200 rounded-lg text-sm text-indigo-800 flex items-start gap-2">
          <Box size={16} className="mt-0.5 shrink-0" />
          <span>Upload 3D visualization files for this design. Accepted formats: <strong>.skp</strong>, <strong>.jpeg</strong>, <strong>.png</strong>.</span>
        </div>

        {/* Already-saved files */}
        {existingFiles.length > 0 && (
          <div>
            <p className="text-xs font-semibold text-gray-500 uppercase tracking-wide mb-2">Uploaded</p>
            <ul className="space-y-1.5">
              {existingFiles.map((f) => (
                <li key={f.id} className="flex items-center gap-2 text-sm bg-gray-50 rounded-lg px-3 py-2">
                  <FileCheck size={14} className="text-green-500 shrink-0" />
                  <a href={f.fileUrl} target="_blank" rel="noopener noreferrer"
                     className="text-blue-600 hover:underline truncate flex-1">
                    {f.fileName}
                  </a>
                  <span className="text-xs text-gray-400 uppercase shrink-0">.{f.fileType}</span>
                </li>
              ))}
            </ul>
          </div>
        )}

        {!isComplete && (
          <>
            {/* Drop zone */}
            <div>
              <input
                ref={inputRef}
                type="file"
                accept={ACCEPTED_TYPES}
                multiple
                className="hidden"
                onChange={handleFileChange}
              />
              <button
                type="button"
                disabled={uploading}
                onClick={() => inputRef.current?.click()}
                className="w-full flex flex-col items-center justify-center gap-2 border-2 border-dashed border-gray-300 rounded-lg px-4 py-5 text-sm text-gray-500 hover:border-indigo-400 hover:text-indigo-600 transition-colors disabled:opacity-50"
              >
                {uploading ? (
                  <span className="animate-pulse">Uploading…</span>
                ) : (
                  <>
                    <UploadCloud size={22} />
                    <span>Click to select files <span className="text-xs text-gray-400">(.skp, .jpeg, .png — multiple allowed)</span></span>
                  </>
                )}
              </button>
            </div>

            {/* Queued (unsaved) files */}
            {queue.length > 0 && (
              <div>
                <p className="text-xs font-semibold text-gray-500 uppercase tracking-wide mb-2">Ready to save ({queue.length})</p>
                <ul className="space-y-1.5">
                  {queue.map((f, idx) => (
                    <li key={idx} className="flex items-center gap-2 text-sm bg-amber-50 border border-amber-200 rounded-lg px-3 py-2">
                      <FileCheck size={14} className="text-amber-500 shrink-0" />
                      <span className="truncate flex-1 text-amber-800">{f.fileName}</span>
                      <span className="text-xs text-amber-400 uppercase shrink-0">.{f.fileType}</span>
                      <button onClick={() => removeQueued(idx)} className="text-red-400 hover:text-red-600 shrink-0">
                        <Trash2 size={13} />
                      </button>
                    </li>
                  ))}
                </ul>
                <Button
                  variant="secondary"
                  className="w-full mt-2"
                  loading={uploading}
                  onClick={handleSaveAll}
                  icon={<UploadCloud size={14} />}
                >
                  Save files
                </Button>
              </div>
            )}

            {/* Complete step */}
            <div className="pt-1 border-t border-gray-100 space-y-2">
              <p className="text-xs text-gray-500">
                Once all 3D files are uploaded, mark this step as complete to advance the workflow.
              </p>
              <Button
                variant="primary"
                className="w-full"
                loading={completing || uploading}
                disabled={existingFiles.length === 0 && queue.length === 0}
                onClick={handleComplete}
                icon={<CheckCircle size={14} />}
              >
                Complete 3D Visualization
              </Button>
            </div>
          </>
        )}

        {isComplete && (
          <div className="p-3 bg-green-50 border border-green-200 rounded-lg text-sm text-green-800 flex items-center gap-2">
            <CheckCircle size={14} />
            3D Visualization step is complete.
          </div>
        )}

        {/* 3D CAD */}
        <CadSection
          cadType="ThreeD"
          label="3D CAD"
          requestId={requestId}
          request={request}
          accentClass="border-indigo-200 bg-indigo-50"
        />
      </div>
    </Card>
  );
};
