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
  const memberInputRef = useRef<HTMLInputElement>(null);
  const dhaInputRef = useRef<HTMLInputElement>(null);
  const [memberFile, setMemberFile] = useState<File | null>(null);
  const [dhaFile, setDhaFile] = useState<File | null>(null);
  const [memberFileUrl, setMemberFileUrl] = useState('');
  const [dhaFileUrl, setDhaFileUrl] = useState('');
  const [comments, setComments] = useState('');
  const [submitting, setSubmitting] = useState(false);

  const handleFileChange = (event: React.ChangeEvent<HTMLInputElement>, copy: 'member' | 'dha') => {
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
    if (copy === 'member') {
      setMemberFile(selectedFile);
      setMemberFileUrl('');
    } else {
      setDhaFile(selectedFile);
      setDhaFileUrl('');
    }
  };

  const handleSubmit = async () => {
    if (!memberFile || !dhaFile) {
      toast.error('Please select both possession letter copies');
      return;
    }

    setSubmitting(true);
    try {
      const uploadFile = async (file: File) => {
        const formData = new FormData();
        formData.append('file', file);
        const response = await api.post<{ url: string }>('/uploads', formData, {
          headers: { 'Content-Type': 'multipart/form-data' },
        });
        return response.data.url;
      };
      let uploadedMemberUrl = memberFileUrl;
      if (!uploadedMemberUrl) {
        uploadedMemberUrl = await uploadFile(memberFile);
        setMemberFileUrl(uploadedMemberUrl);
      }
      let uploadedDhaUrl = dhaFileUrl;
      if (!uploadedDhaUrl) {
        uploadedDhaUrl = await uploadFile(dhaFile);
        setDhaFileUrl(uploadedDhaUrl);
      }
      await dispatch(
        bcdUploadPossessionLetter({
          id: requestId,
          fileUrl: uploadedMemberUrl,
          dhaCopyFileUrl: uploadedDhaUrl,
          comments: comments.trim() || undefined,
        })
      ).unwrap();
      toast.success('Possession letter copies uploaded and forwarded to DDFC Admin');
      setMemberFile(null);
      setDhaFile(null);
      setMemberFileUrl('');
      setDhaFileUrl('');
      setComments('');
    } catch {
      toast.error('Failed to upload possession letter copies');
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <Card title="BCD – Upload Possession Letter">
      <div className="space-y-4">
        <p className="text-sm text-gray-500">
          Upload the member and DHA copies after AD Coordinator approval. This step must be completed
          before DDFC Admin can sign.
        </p>

        <div className="space-y-2">
          <input
            ref={memberInputRef}
            type="file"
            accept=".pdf,.jpg,.jpeg,.png"
            aria-label="Possession letter member copy"
            className="hidden"
            onChange={(event) => handleFileChange(event, 'member')}
            disabled={submitting}
          />
          <Button
            type="button"
            variant="outline"
            disabled={submitting}
            onClick={() => memberInputRef.current?.click()}
            icon={<Upload size={16} />}
          >
            Upload Member Copy
          </Button>
          {memberFile && <p className="break-all text-sm text-gray-700" role="status">{memberFile.name}</p>}
        </div>

        <div className="space-y-2">
          <input
            ref={dhaInputRef}
            type="file"
            accept=".pdf,.jpg,.jpeg,.png"
            aria-label="Possession letter DHA copy"
            className="hidden"
            onChange={(event) => handleFileChange(event, 'dha')}
            disabled={submitting}
          />
          <Button
            type="button"
            variant="outline"
            disabled={submitting}
            onClick={() => dhaInputRef.current?.click()}
            icon={<Upload size={16} />}
          >
            Upload DHA Copy
          </Button>
          {dhaFile && <p className="break-all text-sm text-gray-700" role="status">{dhaFile.name}</p>}
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
          disabled={!memberFile || !dhaFile}
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
