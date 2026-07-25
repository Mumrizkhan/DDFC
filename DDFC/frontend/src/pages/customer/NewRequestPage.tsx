import React, { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { ArrowLeft, Send } from 'lucide-react';
import { useAppDispatch } from '../../store/hooks';
import { createRequest } from '../../store/slices/requestsSlice';
import { Card } from '../../components/ui/Card';
import { Input, Select } from '../../components/ui/Input';
import { Button } from '../../components/ui/Button';
import { toast } from 'react-toastify';
import api from '../../services/api';
import type { Plot } from '../../types';

const schema = z.object({
  fileNo: z.string().min(1, 'File number is required'),
  membershipDPRNo: z.string().min(1, 'Membership/DPR No is required'),
  plotId: z.string().min(1, 'Plot is required'),
  plotNumber: z.string().min(1, 'Plot number is required'),
  sectorNo: z.string().min(1, 'Sector is required'),
  phaseNo: z.string().min(1, 'Phase is required'),
  ownerTitle: z.string().min(1, 'Title is required'),
  ownerName: z.string().min(1, 'Owner name is required'),
  cnic: z.string().regex(/^\d{5}-\d{7}-\d$/, 'Enter valid CNIC: XXXXX-XXXXXXX-X'),
  cellNo: z.string().regex(/^03\d{9}$/, 'Enter valid phone: 03XXXXXXXXX'),
  relationType: z.string().min(1, 'Relation type is required'),
  relationName: z.string().min(1, 'Relation name is required'),
});

type FormValues = z.infer<typeof schema>;

const TITLE_OPTIONS = [
  { value: 'Mr', label: 'Mr' },
  { value: 'Mrs', label: 'Mrs' },
  { value: 'Ms', label: 'Ms' },
  { value: 'Dr', label: 'Dr' },
];

const RELATION_OPTIONS = [
  { value: 'Self', label: 'Self' },
  { value: 'Father', label: 'Father' },
  { value: 'Mother', label: 'Mother' },
  { value: 'Spouse', label: 'Spouse' },
  { value: 'Guardian', label: 'Guardian' },
];

export const NewRequestPage: React.FC = () => {
  const dispatch = useAppDispatch();
  const navigate = useNavigate();
  const [phases, setPhases] = useState<string[]>([]);
  const [sectors, setSectors] = useState<string[]>([]);
  const [plots, setPlots] = useState<Plot[]>([]);
  const [selectedPhase, setSelectedPhase] = useState('');
  const [selectedSector, setSelectedSector] = useState('');
  const [loadingPhases, setLoadingPhases] = useState(false);
  const [loadingSectors, setLoadingSectors] = useState(false);
  const [loadingPlots, setLoadingPlots] = useState(false);

  const {
    register,
    handleSubmit,
    setValue,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({ resolver: zodResolver(schema) });

  // Load phases on mount
  useEffect(() => {
    const load = async () => {
      try {
        setLoadingPhases(true);
        const res = await api.get<string[]>('/admin/plots/phases');
        setPhases(res.data);
      } catch {
        toast.error('Failed to load phases');
      } finally {
        setLoadingPhases(false);
      }
    };
    load();
  }, []);

  // Load sectors when phase changes
  useEffect(() => {
    setSectors([]);
    setPlots([]);
    setSelectedSector('');
    setValue('sectorNo', '');
    setValue('plotId', '');
    setValue('plotNumber', '');
    if (!selectedPhase) return;
    const load = async () => {
      try {
        setLoadingSectors(true);
        const res = await api.get<string[]>('/admin/plots/sectors', { params: { phaseNo: selectedPhase } });
        setSectors(res.data);
      } catch {
        toast.error('Failed to load sectors');
      } finally {
        setLoadingSectors(false);
      }
    };
    load();
  }, [selectedPhase, setValue]);

  // Load plots when phase + sector are selected
  useEffect(() => {
    setPlots([]);
    setValue('plotId', '');
    setValue('plotNumber', '');
    if (!selectedPhase || !selectedSector) return;
    const load = async () => {
      try {
        setLoadingPlots(true);
        const res = await api.get<Plot[]>('/admin/plots', {
          params: { phaseNo: selectedPhase, sectorNo: selectedSector, status: 'Available' },
        });
        setPlots(res.data);
      } catch {
        toast.error('Failed to load plots');
      } finally {
        setLoadingPlots(false);
      }
    };
    load();
  }, [selectedPhase, selectedSector, setValue]);

  const onSubmit = async (data: FormValues) => {
    try {
      const result = await dispatch(createRequest(data as unknown as Parameters<typeof createRequest>[0])).unwrap();
      toast.success('Request submitted successfully!');
      navigate(`/portal/requests/${result.requestId}`);
    } catch {
      toast.error('Failed to submit request. Please try again.');
    }
  };

  return (
    <div className="max-w-2xl mx-auto p-4 space-y-5">
      <div className="flex items-center gap-3">
        <button onClick={() => navigate(-1)} className="text-gray-400 hover:text-gray-700">
          <ArrowLeft size={20} />
        </button>
        <div>
          <h1 className="text-xl font-bold text-gray-900">New Possession Request</h1>
          <p className="text-sm text-gray-400">Fill in Form 1 details to begin the workflow</p>
        </div>
      </div>

      <Card>
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <Input
              label="File Number"
              placeholder="e.g. DHA-F-2024-001"
              error={errors.fileNo?.message}
              {...register('fileNo')}
            />
            <Input
              label="Membership / DPR No"
              placeholder="e.g. DPR-001"
              error={errors.membershipDPRNo?.message}
              {...register('membershipDPRNo')}
            />

            {/* Phase */}
            <div className="flex flex-col">
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Phase <span className="text-red-500">*</span>
              </label>
              <select
                {...register('phaseNo')}
                value={selectedPhase}
                onChange={(e) => {
                  setSelectedPhase(e.target.value);
                  setValue('phaseNo', e.target.value);
                }}
                disabled={loadingPhases}
                className="px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 disabled:bg-gray-100"
              >
                <option value="">
                  {loadingPhases ? 'Loading...' : 'Select phase'}
                </option>
                {phases.map((p) => (
                  <option key={p} value={p}>{p}</option>
                ))}
              </select>
              {errors.phaseNo && (
                <p className="text-red-500 text-xs mt-1">{errors.phaseNo.message}</p>
              )}
            </div>

            {/* Sector */}
            <div className="flex flex-col">
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Sector <span className="text-red-500">*</span>
              </label>
              <select
                {...register('sectorNo')}
                value={selectedSector}
                onChange={(e) => {
                  setSelectedSector(e.target.value);
                  setValue('sectorNo', e.target.value);
                }}
                disabled={!selectedPhase || loadingSectors}
                className="px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 disabled:bg-gray-100"
              >
                <option value="">
                  {!selectedPhase ? 'Select phase first' : loadingSectors ? 'Loading...' : 'Select sector'}
                </option>
                {sectors.map((s) => (
                  <option key={s} value={s}>{s}</option>
                ))}
              </select>
              {errors.sectorNo && (
                <p className="text-red-500 text-xs mt-1">{errors.sectorNo.message}</p>
              )}
            </div>

            {/* Plot No */}
            <div className="flex flex-col sm:col-span-2">
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Plot No <span className="text-red-500">*</span>
              </label>
              <select
                {...register('plotId')}
                disabled={!selectedSector || !selectedPhase || loadingPlots}
                onChange={(e) => {
                  const plot = plots.find((p) => p.id === e.target.value);
                  if (plot) {
                    setValue('plotId', plot.id);
                    setValue('plotNumber', plot.plotNumber);
                  }
                }}
                className="px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 disabled:bg-gray-100"
              >
                <option value="">
                  {!selectedPhase || !selectedSector
                    ? 'Select phase and sector first'
                    : loadingPlots
                      ? 'Loading...'
                      : plots.length === 0
                        ? 'No available plots'
                        : 'Select plot'}
                </option>
                {plots.map((p) => (
                  <option key={p.id} value={p.id}>{p.plotNumber}</option>
                ))}
              </select>
              {errors.plotId && (
                <p className="text-red-500 text-xs mt-1">{errors.plotId.message}</p>
              )}
            </div>

            <Select
              label="Title"
              options={TITLE_OPTIONS}
              error={errors.ownerTitle?.message}
              {...register('ownerTitle')}
            />
            <Input
              label="Owner Full Name"
              placeholder="As per CNIC"
              error={errors.ownerName?.message}
              {...register('ownerName')}
            />
            <Input
              label="CNIC"
              placeholder="XXXXX-XXXXXXX-X"
              error={errors.cnic?.message}
              {...register('cnic')}
            />
            <Input
              label="Mobile Number"
              placeholder="03XXXXXXXXX"
              error={errors.cellNo?.message}
              {...register('cellNo')}
            />
            <Select
              label="Relation to Owner"
              options={RELATION_OPTIONS}
              error={errors.relationType?.message}
              {...register('relationType')}
            />
            <Input
              label="Relation Name"
              placeholder="Name of applicant's relation"
              error={errors.relationName?.message}
              {...register('relationName')}
            />
          </div>

          <div className="bg-amber-50 border border-amber-200 rounded-lg p-3 text-sm text-amber-800">
            <strong>Note:</strong> After submission, your request will be reviewed by the Transfer Branch. 
            You will receive notifications at each stage of the process.
          </div>

          <div className="flex gap-3 pt-2">
            <Button
              variant="primary"
              type="submit"
              loading={isSubmitting}
              icon={<Send size={16} />}
              className="flex-1"
            >
              Submit Request
            </Button>
            <Button variant="ghost" onClick={() => navigate(-1)} type="button">
              Cancel
            </Button>
          </div>
        </form>
      </Card>
    </div>
  );
};
