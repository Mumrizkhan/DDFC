import React, { useEffect, useRef, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { ArrowLeft, Send, Home, RefreshCw, Ruler } from 'lucide-react';
import { useAppDispatch, useAppSelector } from '../../store/hooks';
import { createRequest } from '../../store/slices/requestsSlice';
import { Card } from '../../components/ui/Card';
import { Input } from '../../components/ui/Input';
import { Button } from '../../components/ui/Button';
import { toast } from 'react-toastify';
import api from '../../services/api';
import type { Customer, Plot, PossessionRequest } from '../../types';
import { schema, type FormValues } from '../../components/request/schema';
import { CustomerSearch } from '../../components/request/CustomerSearch';
import { PhaseSelector } from '../../components/request/PhaseSelector';
import { SectorSelector } from '../../components/request/SectorSelector';
import { PlotTypeSelector } from '../../components/request/PlotTypeSelector';
import { PlotSelector } from '../../components/request/PlotSelector';
import { OwnerInfoSection } from '../../components/request/OwnerInfoSection';

// 0 = PossessionDesign, 1 = RevisedPlan, 2 = AsBuiltPlan
type WorkflowType = 0 | 1 | 2;

const WORKFLOW_OPTIONS: { type: WorkflowType; label: string; description: string; icon: React.ReactNode; color: string }[] = [
  { type: 0, label: 'Possession & House Design', description: 'Full NOC/NDC possession workflow', icon: <Home size={18} />, color: 'blue' },
  { type: 1, label: 'Revised Plan',              description: 'Revise an existing approved design', icon: <RefreshCw size={18} />, color: 'amber' },
  { type: 2, label: 'As Built Plan',             description: 'Document as-built construction drawings', icon: <Ruler size={18} />, color: 'emerald' },
];

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

  // Workflow type selection
  const [workflowType, setWorkflowType] = useState<WorkflowType>(0);

  // Linked possession request (for Revised Plan / As Built Plan)
  const [possessionRequests, setPossessionRequests] = useState<PossessionRequest[]>([]);
  const [loadingPossessionRequests, setLoadingPossessionRequests] = useState(false);
  const [linkedRequestId, setLinkedRequestId] = useState('');
  const [linkedRequestError, setLinkedRequestError] = useState('');
  const [linkedRequestInfo, setLinkedRequestInfo] = useState<{ packageTier?: string; designType?: string; packageTotal?: number } | null>(null);

  // Stores pending plot data during linked-request auto-fill cascade
  const autoFillRef = useRef<{ sectorNo: string; plotType: string; plotId: string; plotNumber: string } | null>(null);

  const {
    register,
    handleSubmit,
    formState: { errors },
    reset,
    setValue,
    watch,
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
      sonDaughterWifeOf: '',
      guardianRelation: '',
      contractor: '',
      authorizedPersonName: '',
    },
  });

  // Load customers
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

  // Load phases
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
        if (autoFillRef.current) {
          setSelectedSector(autoFillRef.current.sectorNo);
          setValue('sectorNo', autoFillRef.current.sectorNo);
          setSelectedPlotType(autoFillRef.current.plotType);
        }
      } catch {
        toast.error('Failed to load sectors');
      } finally {
        setLoadingSectors(false);
      }
    };
    load();
  }, [selectedPhase, setValue]);

  // Load plots when phase/sector/plotType changes
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
        if (autoFillRef.current) {
          setSelectedPlotId(autoFillRef.current.plotId);
          setSelectedPlotNumber(autoFillRef.current.plotNumber);
          autoFillRef.current = null;
        }
      } catch {
        toast.error('Failed to load plots');
      } finally {
        setLoadingPlots(false);
      }
    };
    load();
  }, [selectedPhase, selectedSector, selectedPlotType]);

  // Load possession requests for linked workflow when customer changes
  useEffect(() => {
    setPossessionRequests([]);
    setLinkedRequestId('');
    setLinkedRequestInfo(null);
    setLinkedRequestError('');
    if (workflowType === 0 || !selectedCustomerId) return;
    const load = async () => {
      try {
        setLoadingPossessionRequests(true);
        const res = await api.get<PossessionRequest[]>('/requests', {
          params: { customerId: selectedCustomerId, requestType: 0 },
        });
        setPossessionRequests(res.data);
      } catch {
        toast.error('Failed to load possession requests');
      } finally {
        setLoadingPossessionRequests(false);
      }
    };
    load();
  }, [workflowType, selectedCustomerId]);

  // Update linked request info and auto-fill form fields when selection changes
  useEffect(() => {
    if (!linkedRequestId) { setLinkedRequestInfo(null); return; }
    const req = possessionRequests.find((r) => r.id === linkedRequestId);
    if (req) {
      setLinkedRequestInfo({
        packageTier: req.packageTier,
        designType: req.selectedDesignType,
        packageTotal: req.packageTotal ?? undefined,
      });
      setValue('fileNo', req.fileNo);
      setValue('membershipDPRNo', req.membershipDPRNo);
      setValue('ownerTitle', req.ownerTitle ?? '');
      setValue('ownerName', req.ownerName ?? '');
      setValue('sonDaughterWifeOf', req.sonDaughterWifeOf ?? '');
      setValue('guardianRelation', req.guardianRelation ?? '');
      setValue('contractor', req.contractor ?? '');
      if (req.phaseNo) {
        autoFillRef.current = {
          sectorNo: req.sectorNo ?? '',
          plotType: req.plotType ?? '',
          plotId: req.plotId,
          plotNumber: req.plotNumber ?? '',
        };
        setSelectedPhase(req.phaseNo);
        setValue('phaseNo', req.phaseNo);
      }
    }
  }, [linkedRequestId, possessionRequests, setValue]);

  const onSubmit = async (data: FormValues) => {
    if (!selectedPlotType) { setPlotTypeError('Plot type is required'); return; }
    if (!selectedPlotId) { setPlotError('Plot is required'); return; }
    if (workflowType !== 0 && !linkedRequestId) {
      setLinkedRequestError('Please select the linked possession request');
      return;
    }
    setPlotError('');
    setLinkedRequestError('');
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
          sonDaughterWifeOf: data.sonDaughterWifeOf,
          guardianRelation: data.guardianRelation,
          authorizedPersonName: data.authorizedPersonName || undefined,
          contractor: data.contractor || undefined,
          requestType: workflowType,
          linkedPossessionRequestId: workflowType !== 0 ? linkedRequestId : undefined,
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

      {/* ── Workflow Type Selection ── */}
      <Card className="mb-4">
        <p className="text-sm font-medium text-gray-700 mb-3">Select Workflow Type</p>
        <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
          {WORKFLOW_OPTIONS.map(({ type, label, description, icon, color }) => {
            const active = workflowType === type;
            const base = 'flex flex-col gap-1 p-3 rounded-xl border-2 cursor-pointer transition-all text-left';
            const activeStyle =
              color === 'blue'    ? 'border-blue-500 bg-blue-50'   :
              color === 'amber'   ? 'border-amber-500 bg-amber-50' :
                                    'border-emerald-500 bg-emerald-50';
            const inactiveStyle = 'border-gray-200 hover:border-gray-300 bg-white';
            return (
              <button
                key={type}
                type="button"
                onClick={() => { setWorkflowType(type); setLinkedRequestId(''); setLinkedRequestInfo(null); setLinkedRequestError(''); }}
                className={`${base} ${active ? activeStyle : inactiveStyle}`}
              >
                <span className={`flex items-center gap-2 font-semibold text-sm ${
                  active
                    ? color === 'blue' ? 'text-blue-700' : color === 'amber' ? 'text-amber-700' : 'text-emerald-700'
                    : 'text-gray-700'
                }`}>
                  {icon} {label}
                </span>
                <span className="text-xs text-gray-500">{description}</span>
              </button>
            );
          })}
        </div>
      </Card>

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

          {/* ── Linked Possession Request (Revised Plan / As Built Plan) ── */}
          {workflowType !== 0 && (
            <div className="rounded-xl border-2 border-dashed border-gray-300 bg-gray-50 p-4 space-y-3">
              <p className="text-sm font-semibold text-gray-700">
                Link to Possession Request
                <span className="ml-1 text-red-500">*</span>
              </p>
              <p className="text-xs text-gray-500">
                Select the original Possession &amp; House Design request to inherit the package automatically.
              </p>

              {!selectedCustomerId ? (
                <p className="text-xs text-amber-600 italic">Select a customer first to see their possession requests.</p>
              ) : loadingPossessionRequests ? (
                <p className="text-xs text-gray-400">Loading possession requests…</p>
              ) : possessionRequests.length === 0 ? (
                <p className="text-xs text-red-500">No possession requests found for this customer.</p>
              ) : (
                <select
                  className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                  value={linkedRequestId}
                  onChange={(e) => { setLinkedRequestId(e.target.value); setLinkedRequestError(''); }}
                >
                  <option value="">— Select a possession request —</option>
                  {possessionRequests.map((r) => (
                    <option key={r.id} value={r.id}>
                      {r.requestId} · Plot {r.plotNumber} ({r.plotSize}) · {r.status}
                    </option>
                  ))}
                </select>
              )}

              {linkedRequestError && (
                <p className="text-xs text-red-500">{linkedRequestError}</p>
              )}

              {linkedRequestInfo && (
                <div className="rounded-lg bg-white border border-green-200 px-3 py-2 text-xs text-green-800 space-y-0.5">
                  <p className="font-semibold">✓ Package will be auto-selected</p>
                  {linkedRequestInfo.packageTier && <p>Tier: {linkedRequestInfo.packageTier}</p>}
                  {linkedRequestInfo.designType   && <p>Type: {linkedRequestInfo.designType}</p>}
                  {linkedRequestInfo.packageTotal  && (
                    <p>Fee: PKR {linkedRequestInfo.packageTotal.toLocaleString()}</p>
                  )}
                </div>
              )}
            </div>
          )}

          <PhaseSelector
            phases={phases}
            loading={loadingPhases}
            selectedPhase={selectedPhase}
            onSelect={(p) => { setSelectedPhase(p); setValue('phaseNo', p); }}
            error={errors.phaseNo?.message}
          />

          <SectorSelector
            sectors={sectors}
            loading={loadingSectors}
            selectedPhase={selectedPhase}
            selectedSector={selectedSector}
            onSelect={(s) => { setSelectedSector(s); setValue('sectorNo', s); }}
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

          <OwnerInfoSection register={register} errors={errors} watchedRelation={watch('guardianRelation')} />

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