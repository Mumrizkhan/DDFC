import React, { useEffect, useState } from 'react';
import { templatesService } from '../../services/endpoints';
import { Card } from '../../components/ui/Card';
import { Button } from '../../components/ui/Button';
import { Badge } from '../../components/ui/Badge';
import { Modal } from '../../components/ui/Modal';
import { Textarea } from '../../components/ui/Input';
import { PageLoader } from '../../components/ui/LoadingSpinner';
import { Edit2 } from 'lucide-react';
import { toast } from 'react-toastify';
import { format } from 'date-fns';
import type { Template } from '../../types';

export const TemplateManagementPage: React.FC = () => {
  const [templates, setTemplates] = useState<Template[]>([]);
  const [loading, setLoading] = useState(true);
  const [editing, setEditing] = useState<Template | null>(null);
  const [editContent, setEditContent] = useState('');
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    templatesService.getAll()
      .then(setTemplates)
      .catch(() => {})
      .finally(() => setLoading(false));
  }, []);

  const openEdit = (template: Template) => {
    setEditing(template);
    setEditContent(template.content);
  };

  const handleSave = async () => {
    if (!editing) return;
    setSaving(true);
    try {
      const updated = await templatesService.update(editing.templateId, { content: editContent });
      setTemplates((prev) => prev.map((t) => (t.templateId === updated.templateId ? updated : t)));
      toast.success('Template saved');
      setEditing(null);
    } catch {
      toast.error('Failed to save template');
    } finally {
      setSaving(false);
    }
  };

  if (loading) return <PageLoader />;

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-gray-900">Template Management</h1>
        <p className="text-gray-500 text-sm mt-1">
          Manage PDF/Razor templates used for document generation. Only the Survey Form has an Urdu version.
        </p>
      </div>

      <Card>
        <div className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-gray-100">
                <th className="text-left font-medium text-gray-500 pb-3">Template Name</th>
                <th className="text-left font-medium text-gray-500 pb-3">Type</th>
                <th className="text-left font-medium text-gray-500 pb-3">Language</th>
                <th className="text-left font-medium text-gray-500 pb-3">Version</th>
                <th className="text-left font-medium text-gray-500 pb-3">Last Updated</th>
                <th className="text-left font-medium text-gray-500 pb-3">Actions</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-50">
              {templates.map((t) => (
                <tr key={t.templateId} className="hover:bg-gray-50">
                  <td className="py-3 font-medium text-gray-900">{t.templateName}</td>
                  <td className="py-3 text-gray-600">{t.templateType}</td>
                  <td className="py-3">
                    <Badge variant={t.language === 'UR' ? 'secondary' : 'info'}>
                      {t.language === 'UR' ? 'Urdu (RTL)' : 'English'}
                    </Badge>
                  </td>
                  <td className="py-3 text-gray-600">v{t.version}</td>
                  <td className="py-3 text-gray-500">
                    {t.updatedAt ? format(new Date(t.updatedAt), 'dd MMM yyyy') : ''}
                  </td>
                  <td className="py-3">
                    <button
                      onClick={() => openEdit(t)}
                      className="text-blue-600 hover:text-blue-800"
                      title="Edit"
                    >
                      <Edit2 size={14} />
                    </button>
                  </td>
                </tr>
              ))}
              {templates.length === 0 && (
                <tr>
                  <td colSpan={6} className="py-8 text-center text-gray-400">
                    No templates found
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </Card>

      <Modal
        isOpen={!!editing}
        onClose={() => setEditing(null)}
        title={`Edit: ${editing?.templateName}`}
        size="xl"
        footer={
          <>
            <Button variant="outline" onClick={() => setEditing(null)}>Cancel</Button>
            <Button onClick={handleSave} loading={saving}>Save Template</Button>
          </>
        }
      >
        <div className="space-y-3">
          <p className="text-sm text-yellow-700 bg-yellow-50 border border-yellow-200 rounded-lg p-3">
            Editing Razor HTML template. Changes are versioned automatically.
            {editing?.language === 'UR' && ' This is the Urdu (RTL) version.'}
          </p>
          <Textarea
            label="Template Content (Razor HTML)"
            value={editContent}
            onChange={(e) => setEditContent(e.target.value)}
            rows={16}
            className="font-mono text-xs"
          />
        </div>
      </Modal>
    </div>
  );
};
