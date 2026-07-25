import React, { useState } from 'react';
import { Card } from '../../components/ui/Card';
import { Button } from '../../components/ui/Button';
import { Input } from '../../components/ui/Input';
import { toast } from 'react-toastify';

const SETTINGS_KEY = 'ddfc_system_settings';

const SETTINGS_GROUPS = [
  {
    title: 'SMS Gateway (Twilio)',
    keys: ['Twilio_AccountSid', 'Twilio_AuthToken', 'Twilio_FromNumber'],
    labels: ['Account SID', 'Auth Token', 'From Number'],
    types: ['text', 'password', 'text'],
  },
  {
    title: 'Email Gateway (SendGrid)',
    keys: ['SendGrid_ApiKey', 'SendGrid_FromEmail', 'SendGrid_FromName'],
    labels: ['API Key', 'From Email', 'From Name'],
    types: ['password', 'email', 'text'],
  },
  {
    title: 'SLA Thresholds (days)',
    keys: ['SLA_Transfer', 'SLA_Finance', 'SLA_Architecture', 'SLA_Structure', 'SLA_MEP', 'SLA_Survey'],
    labels: ['Transfer Branch', 'Finance Branch', 'Architecture', 'Structure', 'MEP', 'Survey'],
    types: ['number', 'number', 'number', 'number', 'number', 'number'],
  },
  {
    title: 'File Storage',
    keys: ['Storage_LocalPath', 'Storage_AzureBlobConnection'],
    labels: ['Local Storage Path', 'Azure Blob Connection String'],
    types: ['text', 'password'],
  },
  {
    title: 'Audit & Compliance',
    keys: ['Audit_RetentionYears'],
    labels: ['Audit Log Retention (years)'],
    types: ['number'],
  },
  {
    title: 'Plot Modifications',
    keys: ['PlotModification_AnnexationFee'],
    labels: ['Default Annexation Fee (PKR)'],
    types: ['number'],
  },
];

const DEFAULTS: Record<string, string> = {
  PlotModification_AnnexationFee: '15000',
};

const loadSettings = (): Record<string, string> => {
  try {
    const raw = localStorage.getItem(SETTINGS_KEY);
    return { ...DEFAULTS, ...(raw ? JSON.parse(raw) : {}) };
  } catch {
    return { ...DEFAULTS };
  }
};

export const SystemSettingsPage: React.FC = () => {
  const [localSettings, setLocalSettings] = useState<Record<string, string>>(loadSettings);
  const [saving, setSaving] = useState(false);

  const handleChange = (key: string, value: string) => {
    setLocalSettings((prev) => ({ ...prev, [key]: value }));
  };

  const handleSave = () => {
    setSaving(true);
    try {
      localStorage.setItem(SETTINGS_KEY, JSON.stringify(localSettings));
      toast.success('Settings saved locally');
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">System Settings</h1>
          <p className="text-gray-500 text-sm mt-1">
            Configure integrations, SLA thresholds, and system behavior.
          </p>
        </div>
        <Button onClick={handleSave} loading={saving}>
          Save All Settings
        </Button>
      </div>

      {SETTINGS_GROUPS.map((group) => (
        <Card key={group.title} title={group.title}>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            {group.keys.map((key, idx) => (
              <Input
                key={key}
                label={group.labels[idx]}
                type={group.types[idx] as 'text' | 'password' | 'email' | 'number'}
                value={localSettings[key] ?? ''}
                onChange={(e) => handleChange(key, e.target.value)}
              />
            ))}
          </div>
        </Card>
      ))}
    </div>
  );
};
