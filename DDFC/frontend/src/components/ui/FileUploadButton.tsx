import React, { useRef, useState } from 'react';
import { UploadCloud, FileCheck } from 'lucide-react';
import api from '../../services/api';
import { toast } from 'react-toastify';

interface Props {
  /** Called with the stored URL once upload succeeds */
  onUploaded: (url: string) => void;
  /** Clears the uploaded state when parent resets the form */
  value?: string;
  label?: string;
  required?: boolean;
  accept?: string;
}

export const FileUploadButton: React.FC<Props> = ({
  onUploaded,
  value,
  label,
  required,
  accept = '.pdf,.jpg,.jpeg,.png',
}) => {
  const [uploading, setUploading] = useState(false);
  const [fileName, setFileName]   = useState('');
  const inputRef = useRef<HTMLInputElement>(null);

  // When parent resets value to '' clear the displayed filename
  React.useEffect(() => {
    if (!value) setFileName('');
  }, [value]);

  const handleChange = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;
    setUploading(true);
    try {
      const formData = new FormData();
      formData.append('file', file);
      const res = await api.post<{ url: string; originalName: string }>('/uploads', formData, {
        headers: { 'Content-Type': 'multipart/form-data' },
      });
      setFileName(res.data.originalName);
      onUploaded(res.data.url);
      toast.success('File uploaded');
    } catch {
      toast.error('Upload failed — please try again');
    } finally {
      setUploading(false);
      if (inputRef.current) inputRef.current.value = '';
    }
  };

  return (
    <div>
      {label && (
        <p className="text-sm font-medium text-gray-700 mb-1">
          {label} {required && <span className="text-red-500">*</span>}
        </p>
      )}
      <input ref={inputRef} type="file" accept={accept} className="hidden" onChange={handleChange} />
      <button
        type="button"
        onClick={() => inputRef.current?.click()}
        disabled={uploading}
        className="w-full flex items-center justify-center gap-2 border-2 border-dashed border-gray-300 rounded-lg px-4 py-3 text-sm text-gray-600 hover:border-blue-400 hover:text-blue-600 transition-colors disabled:opacity-50"
      >
        {uploading ? (
          <span className="animate-pulse">Uploading…</span>
        ) : fileName ? (
          <>
            <FileCheck size={16} className="text-green-600 shrink-0" />
            <span className="text-green-700 font-medium truncate max-w-xs">{fileName}</span>
            <span className="text-xs text-gray-400 ml-1 shrink-0">(click to replace)</span>
          </>
        ) : (
          <>
            <UploadCloud size={16} />
            Click to upload PDF / JPG / PNG
          </>
        )}
      </button>
    </div>
  );
};
