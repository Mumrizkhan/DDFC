import React, { useRef, useState } from 'react';
import { Upload } from 'lucide-react';
import { Card } from '../../../components/ui/Card';
import { Button } from '../../../components/ui/Button';
import { Input } from '../../../components/ui/Input';
import { useAppDispatch } from '../../../store/hooks';
import { bcdUploadPossessionLetter } from '../../../store/slices/requestsSlice';
import { toast } from 'react-toastify';
import api from '../../../services/api';

interface Props {
  requestId: string;
}

export const BcdPanel: React.FC<Props> = ({ requestId }) => {
  const dispatch = useAppDispatch();
  const inputRef = useRef<HTMLInputElement>(null);
  const [file, setFile] = useState<File | null>(null);
  const [fileUrl, setFileUrl] = useState('');
  const [comments, setComments] = useState('');
  const [submitting, setSubmitting] = useState(false);

  const handleFileChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    const selectedFile = event.target.files?.[0];
    event.target.value = '';
    if (!selectedFile) return;
    if (!/\.(pdf|jpe?g|png)$/i.test(selectedFile.name)) {
      toast.error('Only PDF, JPG, and PNG files are allowed');
      return;
    }
    if (selectedFile.size === 0 || selectedFile.size >= 20 * 1024 * 1024) {
      toast.error('Choose a non-empty file smaller than 20 MB');
      return;
    }
    setFile(selectedFile);
    setFileUrl('');
  };

  const handleSubmit = async () => {
    if (!file) {
      toast.error('Please select a possession letter file');
      return;
    }

    setSubmitting(true);
    try {
      let uploadedUrl = fileUrl;
      if (!uploadedUrl) {
        const formData = new FormData();
        formData.append('file', file);
        const response = await api.post<{ url: string }>('/uploads', formData, {
          headers: { 'Content-Type': 'multipart/form-data' },
        });
        uploadedUrl = response.data.url;
        setFileUrl(uploadedUrl);
      }
      await dispatch(
        bcdUploadPossessionLetter({
          id: requestId,
          fileUrl: uploadedUrl,
          comments: comments.trim() || undefined,
        })
      ).unwrap();
      toast.success('Possession letter uploaded and forwarded to DDFC Admin');
      setFile(null);
      setFileUrl('');
      setComments('');
    } catch {
      toast.error('Failed to upload possession letter');
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <Card title="BCD – Upload Possession Letter">
      <div className="space-y-4">
        <p className="text-sm text-gray-500">
          Upload the possession letter after AD Coordinator approval. This step must be completed
          before DDFC Admin can sign.
        </p>

        <div className="space-y-2">
          <input
            ref={inputRef}
            type="file"
            accept=".pdf,.jpg,.jpeg,.png"
            aria-label="Possession letter file"
            className="hidden"
            onChange={handleFileChange}
            disabled={submitting}
          />
          <Button
            type="button"
            variant="outline"
            disabled={submitting}
            onClick={() => inputRef.current?.click()}
            icon={<Upload size={16} />}
          >
            Upload Possession Letter
          </Button>
          {file && <p className="break-all text-sm text-gray-700" role="status">{file.name}</p>}
        </div>

        <Input
          label="Comments (optional)"
          placeholder="Notes about this upload"
          value={comments}
          onChange={(e) => setComments(e.target.value)}
        />

        <Button
          variant="primary"
          className="w-full"
          disabled={!file}
          loading={submitting}
          onClick={handleSubmit}
          icon={<Upload size={16} />}
        >
          Submit &amp; Send to DDFC Admin
        </Button>
      </div>
    </Card>
  );
};
