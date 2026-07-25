import React, { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { ArrowLeft, Send } from 'lucide-react';
import { useAppDispatch, useAppSelector } from '../../store/hooks';
import { createRequest } from '../../store/slices/requestsSlice';
import { Card } from '../../components/ui/Card';
import { Input } from '../../components/ui/Input';
import { Button } from '../../components/ui/Button';
import { toast } from 'react-toastify';
import api from '../../services/api';
import type { Customer, Plot } from '../../types';
import { schema, type FormValues } from '../../components/request/schema';
import { CustomerSearch } from '../../components/request/CustomerSearch';
import { PhaseSelector } from '../../components/request/PhaseSelector';
import { SectorSelector } from '../../components/request/SectorSelector';
import { PlotTypeSelector } from '../../components/request/PlotTypeSelector';
import { PlotSelector } from '../../components/request/PlotSelector';
import { OwnerInfoSection } from '../../components/request/OwnerInfoSection';

export const CreateRequestPage: React.FC = () => {
  const dispatch = useAppDispatch();
  const navigate = useNavigate();
  const { loading } = useAppSelector((s) => s.requests);
  const [customers, setCustomers] = useState<Customer[]>([]);
  const [plots, setPlots] = useState<Plot[]>([]);
  const [phases, setPhases] = useState<string[]>([]);
  const [sectors, setSectors] = useState<string[]>([]);
  const [selectedCustomerId, setSelectedCustomerId] = useState('');
  const [selectedPhase, setSelectedPhase] = useState('');
  const [selectedSector, setSelectedSector] = useState('');
  const [selectedPlotType, setSelectedPlotType] = useState('');
  const [selectedPlotId, setSelectedPlotId] = useState('');
  const [selectedPlotNumber, setSelectedPlotNumber] = useState('');
  const [loadingCustomers, setLoadingCustomers] = useState(false);
  const [loadingPlots, setLoadingPlots] = useState(false);
  const [loadingPhases, setLoadingPhases] = useState(false);
  const [loadingSectors, setLoadingSectors] = useState(false);
  const [plotError, setPlotError] = useState('');
  const [plotTypeError, setPlotTypeError] = useState('');

  const {
    register,
    handleSubmit,
    formState: { errors },
    reset,
    setValue,
  } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: {
      customerId: '',
      sectorNo: '',
      phaseNo: '',
      fileNo: '',
      membershipDPRNo: '',
      ownerTitle: '',
      ownerName: '',
      guardianName: '',
      guardianRelation: '',
      contractor: '',
    },
  });

  useEffect(() => {
    const load = async () => {
      try {
        setLoadingCustomers(true);
        const res = await api.get<Customer[]>('/admin/customers');
        setCustomers(res.data);
      } catch {
        toast.error('Failed to load customers');
      } finally {
        setLoadingCustomers(false);
      }
    };
    load();
  }, []);

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

  useEffect(() => {
    setSectors([]);
    setPlots([]);
    setSelectedSector('');
    setSelectedPlotId('');
    setSelectedPlotNumber('');
    setPlotError('');
    setValue('sectorNo', '');
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

  useEffect(() => {
    setPlots([]);
    setSelectedPlotId('');
    setSelectedPlotNumber('');
    setPlotError('');
    if (!selectedPhase || !selectedSector || !selectedPlotType) return;
    const load = async () => {
      try {
        setLoadingPlots(true);
        const res = await api.get<Plot[]>('/admin/plots', {
          params: { phaseNo: selectedPhase, sectorNo: selectedSector, status: 'Available', plotType: selectedPlotType },
        });
        setPlots(res.data);
      } catch {
        toast.error('Failed to load plots');
      } finally {
        setLoadingPlots(false);
      }
    };
    load();
  }, [selectedPhase, selectedSector, selectedPlotType]);

  const onSubmit = async (data: FormValues) => {
    if (!selectedPlotType) {
      setPlotTypeError('Plot type is required');
      return;
    }
    if (!selectedPlotId) {
      setPlotError('Plot is required');
      return;
    }
    setPlotError('');
    try {
      await dispatch(
        createRequest({
          customerId: data.customerId,
          plotId: selectedPlotId,
          plotNumber: selectedPlotNumber,
          sectorNo: data.sectorNo,
          phaseNo: data.phaseNo,
          fileNo: data.fileNo,
          membershipDPRNo: data.membershipDPRNo,
          ownerTitle: data.ownerTitle,
          ownerName: data.ownerName,
          guardianName: data.guardianName,
          guardianRelation: data.guardianRelation,
          contractor: data.contractor || undefined,
        })
      ).unwrap();
      toast.success('Request created successfully!');
      reset();
      navigate('/backoffice/requests');
    } catch (err) {
      const msg = typeof err === 'string' ? err : (err as { message?: string })?.message;
      toast.error(msg || 'Failed to create request');
    }
  };

  // Key forces PlotSelector to remount (clear internal state) when dependencies change
  const plotSelectorKey = `${selectedPhase}-${selectedSector}-${selectedPlotType}`;

  return (
    <div className="max-w-2xl mx-auto">
      <div className="mb-6 flex items-center gap-3">
        <button
          onClick={() => navigate('/backoffice/requests')}
          className="p-2 hover:bg-gray-100 rounded-lg transition-colors"
        >
          <ArrowLeft size={20} className="text-gray-600" />
        </button>
        <h1 className="text-2xl font-bold text-gray-900">Create New Request</h1>
      </div>

      <Card>
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-6 text-left">
          <CustomerSearch
            customers={customers}
            loadingCustomers={loadingCustomers}
            selectedCustomerId={selectedCustomerId}
            onSelectCustomer={(id) => {
              setSelectedCustomerId(id);
              setValue('customerId', id);
            }}
            onCustomerAdded={(customer) => {
              setCustomers((prev) => [...prev, customer]);
            }}
            error={errors.customerId?.message}
          />

          <PhaseSelector
            phases={phases}
            loading={loadingPhases}
            selectedPhase={selectedPhase}
            onSelect={(p) => {
              setSelectedPhase(p);
              setValue('phaseNo', p);
            }}
            error={errors.phaseNo?.message}
          />

          <SectorSelector
            sectors={sectors}
            loading={loadingSectors}
            selectedPhase={selectedPhase}
            selectedSector={selectedSector}
            onSelect={(s) => {
              setSelectedSector(s);
              setValue('sectorNo', s);
            }}
            error={errors.sectorNo?.message}
          />

          <PlotTypeSelector
            selectedPlotType={selectedPlotType}
            onChange={(type) => {
              setSelectedPlotType(type);
              setSelectedPlotId('');
              setSelectedPlotNumber('');
              setPlotError('');
              setPlotTypeError('');
            }}
            error={plotTypeError}
          />

          <PlotSelector
            key={plotSelectorKey}
            plots={plots}
            loadingPlots={loadingPlots}
            disabled={!selectedPhase || !selectedSector || !selectedPlotType}
            disabledReason={
              !selectedPhase || !selectedSector
                ? 'Select phase and sector first'
                : 'Select plot type first'
            }
            error={plotError}
            onPlotChange={(plotId, plotNumber) => {
              setSelectedPlotId(plotId);
              setSelectedPlotNumber(plotNumber);
              setPlotError('');
            }}
          />

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              File Number <span className="text-red-500">*</span>
            </label>
            <Input
              {...register('fileNo')}
              placeholder="e.g., F-001234"
              error={errors.fileNo?.message}
            />
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Membership/DPR No <span className="text-red-500">*</span>
            </label>
            <Input
              {...register('membershipDPRNo')}
              placeholder="e.g., MEM-5678"
              error={errors.membershipDPRNo?.message}
            />
          </div>

          <OwnerInfoSection register={register} errors={errors} />

          <div className="flex gap-3 pt-4 border-t">
            <Button variant="secondary" onClick={() => navigate('/backoffice/requests')}>
              Cancel
            </Button>
            <Button type="submit" disabled={loading} icon={<Send size={16} />}>
              {loading ? 'Creating...' : 'Create Request'}
            </Button>
          </div>
        </form>
      </Card>
    </div>
  );
};
