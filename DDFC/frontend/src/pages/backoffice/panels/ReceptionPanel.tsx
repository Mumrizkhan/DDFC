import React, { useEffect, useState } from 'react';
import { ClipboardList, CheckCircle, Package, ChevronRight, CreditCard, Printer, Banknote, Home, Layers, HardHat } from 'lucide-react';
import { Card } from '../../../components/ui/Card';
import { Button } from '../../../components/ui/Button';
import { Input } from '../../../components/ui/Input';
import { FileUploadButton } from '../../../components/ui/FileUploadButton';
import type { PossessionRequest, Package as PkgType } from '../../../types';
import { useAppDispatch, useAppSelector } from '../../../store/hooks';
import { selectPackage, submitPaymentChallan, approvePayment, deliverRequest, attachDocument, adminReview, initiateRequest } from '../../../store/slices/requestsSlice';
import { packagesService, requestsService } from '../../../services/endpoints';
import api from '../../../services/api';
import { toast } from 'react-toastify';

const TIER_COLORS: Record<string, string> = {
  Bronze: 'border-amber-400 bg-amber-50',
  Silver: 'border-gray-400 bg-gray-50',
  Gold:   'border-yellow-400 bg-yellow-50',
};

const ACCENT_RING: Record<string, string> = {
  blue:   'ring-blue-500',
  indigo: 'ring-indigo-500',
  orange: 'ring-orange-500',
};

interface PackagePickerGroupProps {
  packages: PkgType[];
  selectedId: string;
  onSelect: (id: string) => void;
  accentColor?: 'blue' | 'indigo' | 'orange';
  allowDeselect?: boolean;
}

const PackagePickerGroup: React.FC<PackagePickerGroupProps> = ({
  packages, selectedId, onSelect, accentColor = 'blue',
}) => (
  <div className="space-y-2">
    {packages.map((pkg) => {
      const totalDDFC = pkg.lineItems.reduce((s, li) => s + li.amountDDFC, 0);
      const pkgId = pkg.id ?? pkg.packageId;
      const isSelected = selectedId === pkgId;
      return (
        <button
          key={pkgId}
          type="button"
          onClick={() => onSelect(pkgId)}
          className={`w-full text-left border-2 rounded-lg p-3 transition-all ${
            TIER_COLORS[pkg.packageTier] ?? 'border-gray-200 bg-white'
          } ${isSelected ? `ring-2 ${ACCENT_RING[accentColor]}` : 'hover:shadow-sm'}`}
        >
          <div className="flex items-center justify-between mb-1">
            <span className="font-bold text-gray-800">{pkg.packageTier} Tier</span>
            <span className="font-semibold text-gray-800 text-sm">PKR {totalDDFC.toLocaleString()}</span>
          </div>
          <ul className="space-y-0.5">
            {pkg.lineItems.sort((a, b) => a.sortOrder - b.sortOrder).map((li) => (
              <li key={li.id ?? li.lineItemId} className="flex justify-between text-xs text-gray-500">
                <span className="flex items-center gap-1">
                  <ChevronRight className="w-3 h-3 text-gray-300" />
                  {li.serviceName}
                </span>
                <span>{li.isFree ? <span className="text-green-600">Free</span> : `PKR ${li.amountDDFC.toLocaleString()}`}</span>
              </li>
            ))}
          </ul>
          {isSelected && <p className="text-xs font-semibold mt-2" style={{ color: accentColor === 'blue' ? '#2563eb' : accentColor === 'indigo' ? '#4f46e5' : '#ea580c' }}>✓ Selected</p>}
        </button>
      );
    })}
  </div>
);

interface Props {
  request: PossessionRequest;
  requestId: string;
}

// Helper to pick one package per tier from a list
const uniqueByTier = (pkgs: PkgType[]): PkgType[] => {
  const seen = new Set<string>();
  return pkgs.filter((p) => {
    if (seen.has(p.packageTier)) return false;
    seen.add(p.packageTier);
    return true;
  });
};

export const ReceptionPanel: React.FC<Props> = ({ request, requestId }) => {
  const dispatch = useAppDispatch();
  const { staffUser } = useAppSelector((s) => s.auth);

  const [designTrack, setDesignTrack] = useState<'InclusiveDesign' | 'ExclusiveDesign'>('ExclusiveDesign');

  const [houseDesignPkgs,   setHouseDesignPkgs]   = useState<PkgType[]>([]);
  const [interiorPkgs,      setInteriorPkgs]       = useState<PkgType[]>([]);
  const [supervisionPkgs,   setSupervisionPkgs]    = useState<PkgType[]>([]);
  const [loadingPkgs,       setLoadingPkgs]        = useState(false);

  const [selectedPkgId,           setSelectedPkgId]           = useState('');
  const [selectedInteriorPkgId,   setSelectedInteriorPkgId]   = useState('');
  const [selectedSupervisionPkgId, setSelectedSupervisionPkgId] = useState('');
  const [submitting, setSubmitting] = useState(false);

  // Payment step state
  const [challanNo,          setChallanNo]          = useState(request.challanNo ?? '');
  const [scannedFileUrl,     setScannedFileUrl]      = useState('');
  const [paymentSubmitting,  setPaymentSubmitting]   = useState(false);
  const [approving,          setApproving]           = useState(false);

  const [delivering,       setDelivering]       = useState(false);
  const [verifyingDocuments, setVerifyingDocuments] = useState(false);
  const [followUpType, setFollowUpType] = useState<'Cnic' | 'AllotmentLetter' | 'NocNdcForm' | 'MessageScreenshot' | 'EStampPaper' | 'AuthorizedPersonCnic' >('Cnic');
  const [followUpUrl, setFollowUpUrl] = useState('');
  const [followUpSubmitting, setFollowUpSubmitting] = useState(false);

  const activeSteps = request.activeWorkflowStepNames ?? [];
  const isPackageSelectionReady = activeSteps.length > 0
    ? activeSteps.some((name) => name.toLowerCase().includes('package selection'))
    : request.status === 'PossessionLetterSigned';
  const isAwaitingPayment       = request.status === 'PackageSelected';
  // Challan uploaded by Reception; awaiting Possession Admin approval
  const isPaymentPendingApproval = request.paymentStatus === 'PendingApproval';
  const canApprovePayment       = ['Admin', 'Possession Admin', 'DDFC Admin'].includes(staffUser?.roleName ?? '');
  const isFinalApproved         = request.status === 'FinalApproved';
  // History is sorted ascending by timestamp, so the last entry is the most recent action.
  const latestHistoryEntry      = request.workflowHistory?.[request.workflowHistory.length - 1];
  const isIncompleteFollowUp    = request.status === 'Submitted' &&
                                 latestHistoryEntry?.actionBy === 'AdminReview-Incomplete';
  const isPaymentDone      = request.status !== 'Submitted' &&
                              request.status !== 'DocumentsVerification' &&
                              request.status !== 'PossessionIssued' &&
                              request.status !== 'PossessionLetterSigned' &&
                              request.status !== 'BcdLetterUploaded' &&
                              request.status !== 'BothBranchesCleared' &&
                              request.status !== 'AdminReviewPending' &&
                              request.status !== 'AdCoordApproved' &&
                              request.status !== 'TransferApproved' &&
                              request.status !== 'FinanceApproved' &&
                              request.status !== 'PackageSelected';

  useEffect(() => {
    if (!isPackageSelectionReady) return;
    const load = async () => {
      setLoadingPkgs(true);
      try {
        const params = {
          plotType: request.plotType as string | undefined,
          plotSize: request.plotSize as string | undefined,
          designType: designTrack,
        };
        const [hdPkgs, intPkgs, supPkgs] = await Promise.all([
          packagesService.getAll({ ...params, category: 'HouseDesign' }),
          packagesService.getAll({ ...params, category: 'InteriorDesign' }),
          packagesService.getAll({ ...params, category: 'Supervision' }),
        ]);
        setHouseDesignPkgs(uniqueByTier(hdPkgs));
        setInteriorPkgs(uniqueByTier(intPkgs));
        setSupervisionPkgs(uniqueByTier(supPkgs));
        // Reset selections when track changes
        setSelectedPkgId('');
        setSelectedInteriorPkgId('');
        setSelectedSupervisionPkgId('');
      } catch {
        toast.error('Failed to load packages');
      } finally {
        setLoadingPkgs(false);
      }
    };
    load();
  }, [isPackageSelectionReady, request.plotType, request.plotSize, designTrack]);

  useEffect(() => {
    if (request.challanNo) setChallanNo(request.challanNo);
  }, [request.challanNo]);

  const handleSelectPackage = async () => {
    if (!selectedPkgId) return;
    setSubmitting(true);
    try {
      await dispatch(selectPackage({
        id:                        requestId,
        packageId:                 selectedPkgId,
        interiorDesignPackageId:   selectedInteriorPkgId  || undefined,
        supervisionPackageId:      selectedSupervisionPkgId || undefined,
      })).unwrap();
      toast.success('Package confirmed — request moved to payment step');
      // Auto-open the print challan popup as the step completion action
      await handlePrintChallan();
    } catch {
      toast.error('Failed to select package');
    } finally {
      setSubmitting(false);
    }
  };

  const handlePrintCertificate = async () => {
    try {
      const res = await api.get<string>(`/requests/${requestId}/possession-certificate/preview`, {
        responseType: 'text',
      });
      const win = window.open('', '_blank', 'noopener,noreferrer');
      if (!win) {
        toast.warn('Popup blocked — please allow popups and try again');
        return;
      }
      win.document.write(res.data);
      win.document.close();
    } catch {
      toast.error('Failed to load certificate preview');
    }
  };

  const handleDeliver = async () => {
    setDelivering(true);
    try {
      await dispatch(deliverRequest({ id: requestId })).unwrap();
      toast.success('Documents handed over — request marked as Delivered');
    } catch {
      toast.error('Failed to mark as delivered');
    } finally {
      setDelivering(false);
    }
  };

  const handleResubmitIncompleteDocument = async () => {
    if (!followUpUrl.trim()) {
      toast.error('Please upload the corrected document first');
      return;
    }

    setFollowUpSubmitting(true);
    try {
      await dispatch(attachDocument({
        id: requestId,
        documentType: followUpType,
        fileUrl: followUpUrl,
      })).unwrap();

      const payload: {
        action: string;
        allotmentLetterUrl?: string;
        cnicUrl?: string;
        messageScreenshotUrl?: string;
        eStampPaperUrl?: string;
        authorizedPersonCnicUrl?: string;
      } = {
        action: 'Initiate',
        allotmentLetterUrl: followUpType === 'AllotmentLetter' ? followUpUrl : request.allotmentLetterUrl,
        cnicUrl: followUpType === 'Cnic' ? followUpUrl : request.cnicUrl,
        messageScreenshotUrl: followUpType === 'MessageScreenshot' ? followUpUrl : request.messageScreenshotUrl,
        eStampPaperUrl: followUpType === 'EStampPaper' ? followUpUrl : request.eStampPaperUrl,
        authorizedPersonCnicUrl: followUpType === 'AuthorizedPersonCnic' ? followUpUrl : request.authorizedPersonCnicUrl,
      };

      await dispatch(adminReview({ id: requestId, data: payload })).unwrap();
      toast.success('Document resubmitted and sent for admin review');
      setFollowUpUrl('');
    } catch {
      toast.error('Failed to resubmit the document');
    } finally {
      setFollowUpSubmitting(false);
    }
  };

  const handlePrintChallan = async () => {
    // Open popup synchronously (before async) to avoid browser blocking
    const win = window.open('', '_blank');
    if (!win) { toast.error('Please allow popups for this site and try again'); return; }
    win.document.write('<html><body style="font-family:Arial;padding:20px"><p>Loading challan…</p></body></html>');
    try {
      const html = await requestsService.printPaymentChallan(requestId);
      win.document.open();
      win.document.write(html);
      win.document.close();
    } catch {
      win.close();
      toast.error('Failed to load challan');
    }
  };

  const handleConfirmPayment = async () => {
    if (!challanNo.trim()) { toast.error('Challan number is required'); return; }
    if (!scannedFileUrl) { toast.error('Please upload the scanned paid challan'); return; }
    setPaymentSubmitting(true);
    try {
      await dispatch(submitPaymentChallan({
        id: requestId,
        data: { challanNo: challanNo.trim(), scannedChallanFileUrl: scannedFileUrl.trim() },
      })).unwrap();
      toast.success('Challan submitted — awaiting Possession Admin approval');
    } catch {
      toast.error('Failed to submit challan');
    } finally {
      setPaymentSubmitting(false);
    }
  };

  const handleApprovePayment = async () => {
    setApproving(true);
    try {
      await dispatch(approvePayment({ id: requestId })).unwrap();
      toast.success('Payment approved — request advanced to next step');
    } catch {
      toast.error('Failed to approve payment');
    } finally {
      setApproving(false);
    }
  };

  const handleVerifyDocuments = async () => {
    setVerifyingDocuments(true);
    try {
      await dispatch(initiateRequest({ id: requestId, comments: undefined })).unwrap();
      toast.success('Documents verified — request sent to Transfer Branch');
    } catch {
      toast.error('Failed to verify documents');
    } finally {
      setVerifyingDocuments(false);
    }
  };

  return (
    <Card title={isPackageSelectionReady ? 'Package Selection' : 'Reception Panel'}>
      <div className="space-y-5">
        {request.status === 'DocumentsVerification' && (
          <div className="rounded-lg border border-blue-200 bg-blue-50 p-4 space-y-3">
            <p className="text-sm text-blue-800">
              Review the submitted documents, then verify them to send the request to Transfer Branch.
            </p>
            <Button
              variant="primary"
              loading={verifyingDocuments}
              onClick={handleVerifyDocuments}
              icon={<CheckCircle size={16} />}
            >
              Verify Documents
            </Button>
          </div>
        )}
        {isIncompleteFollowUp && (
          <div className="rounded-lg border border-amber-200 bg-amber-50 p-4 space-y-4">
            <div>
              <p className="text-sm font-semibold text-amber-800">Incomplete document follow-up</p>
              <p className="text-xs text-amber-700">This request was returned for correction. Upload the missing document and resubmit it for admin review.</p>
            </div>

            <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Document type</label>
                <select
                  value={followUpType}
                  onChange={(e) => setFollowUpType(e.target.value as typeof followUpType)}
                  className="w-full rounded-lg border border-gray-300 bg-white px-3 py-2 text-sm text-gray-700 outline-none focus:border-blue-400"
                >
                  <option value="Cnic">CNIC</option>
                  <option value="AllotmentLetter">Allotment Letter</option>
                  <option value="NocNdcForm">NOC/NDC Form</option>
                  <option value="MessageScreenshot">Message Screenshot</option>
                  <option value="EStampPaper">E-Stamp Paper</option>
                  <option value="AuthorizedPersonCnic">Authorized Person CNIC</option>
                </select>
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Corrected file</label>
                <input
                  type="file"
                  accept=".pdf,.jpg,.jpeg,.png"
                  onChange={async (e) => {
                    const file = e.target.files?.[0];
                    if (!file) return;
                    try {
                      const formData = new FormData();
                      formData.append('file', file);
                      const res = await api.post<{ url: string; originalName: string }>('/uploads', formData, {
                        headers: { 'Content-Type': 'multipart/form-data' },
                      });
                      setFollowUpUrl(res.data.url);
                      toast.success('File uploaded');
                    } catch {
                      toast.error('Upload failed');
                    }
                  }}
                  className="w-full rounded-lg border border-gray-300 bg-white px-3 py-2 text-sm text-gray-700"
                />
              </div>
            </div>

            <div className="flex items-center justify-between gap-3 border-t border-amber-200 pt-3">
              <span className="text-xs text-amber-700">
                {followUpUrl ? 'Ready to resubmit' : 'No file selected yet'}
              </span>
              <Button
                variant="primary"
                size="sm"
                loading={followUpSubmitting}
                onClick={handleResubmitIncompleteDocument}
              >
                Resubmit to Admin
              </Button>
            </div>
          </div>
        )}

        {/* Form 1 summary */}
        <div className="bg-gray-50 rounded-lg p-4">
          <h3 className="text-sm font-semibold text-gray-700 mb-2 flex items-center gap-2">
            <ClipboardList size={14} />
            Form 1 Summary
          </h3>
          <dl className="grid grid-cols-2 gap-x-4 gap-y-2 text-xs">
            {[
              ['File No', request.fileNo],
              ['CNIC', request.cnic],
              ['Phone', request.phoneNumber],
              ['Plot', request.plotNumber],
              ['Sector', request.sectorNo],
              ['Plot Type', request.plotType],
              ['Plot Size', request.plotSize],
            ].map(([k, v]) => (
              <React.Fragment key={k}>
                <dt className="text-gray-400">{k}</dt>
                <dd className="font-medium text-gray-700">{v ?? '—'}</dd>
              </React.Fragment>
            ))}
          </dl>
        </div>

        {/* ── Package Selection section header ── */}
        {isPackageSelectionReady && (
          <div className="space-y-5">
            <div className="rounded-lg border border-indigo-200 bg-indigo-50 px-4 py-2 flex items-center gap-2">
              <Package size={14} className="text-indigo-600" />
              <span className="text-sm font-semibold text-indigo-800">Step: Package Selection</span>
              <span className="ml-auto text-xs text-indigo-500">Select a package, then print the challan to complete this step</span>
            </div>
            {loadingPkgs && (
              <p className="text-sm text-gray-400 text-center py-4">Loading packages…</p>
            )}

            {/* ── Design Track Selector ── */}
            {!loadingPkgs && (
              <div>
                <p className="text-xs font-semibold text-gray-500 uppercase tracking-wide mb-2">Design Track</p>
                <div className="grid grid-cols-2 gap-2">
                  {([
                    { value: 'InclusiveDesign' as const, label: 'Inclusive Design', sub: 'Premium bespoke designs, includes interior design' },
                    { value: 'ExclusiveDesign' as const, label: 'Exclusive Design', sub: 'Standard inclusive pricing' },
                  ]).map((dt) => (
                    <button
                      key={dt.value}
                      type="button"
                      onClick={() => setDesignTrack(dt.value)}
                      className={`text-left p-3 rounded-lg border-2 transition-all
                        ${designTrack === dt.value
                          ? 'border-blue-500 bg-blue-50'
                          : 'border-gray-200 hover:bg-gray-50'}`}
                    >
                      <p className={`text-xs font-semibold ${designTrack === dt.value ? 'text-blue-700' : 'text-gray-700'}`}>{dt.label}</p>
                      <p className="text-xs text-gray-400 mt-0.5">{dt.sub}</p>
                    </button>
                  ))}
                </div>
              </div>
            )}

            {/* ── 1. House Design Package (mandatory) ── */}
            {!loadingPkgs && (
              <div>
                <h3 className="text-sm font-semibold text-gray-700 mb-3 flex items-center gap-2">
                  <Home size={14} />
                  House Design Package <span className="text-red-500 text-xs">*</span>
                </h3>
                {houseDesignPkgs.length === 0 ? (
                  <p className="text-sm text-gray-400">No packages available for this plot.</p>
                ) : (
                  <PackagePickerGroup
                    packages={houseDesignPkgs}
                    selectedId={selectedPkgId}
                    onSelect={setSelectedPkgId}
                    accentColor="blue"
                  />
                )}
              </div>
            )}

            {/* ── 2. Interior Design Package (optional) ── */}
            {!loadingPkgs && interiorPkgs.length > 0 && (
              <div>
                <h3 className="text-sm font-semibold text-gray-700 mb-1 flex items-center gap-2">
                  <Layers size={14} />
                  Interior Design Package <span className="text-xs text-gray-400 font-normal">(optional)</span>
                </h3>
                <p className="text-xs text-gray-400 mb-3">Add interior design services to your package.</p>
                <PackagePickerGroup
                  packages={interiorPkgs}
                  selectedId={selectedInteriorPkgId}
                  onSelect={(id) => setSelectedInteriorPkgId((prev) => prev === id ? '' : id)}
                  accentColor="indigo"
                  allowDeselect
                />
              </div>
            )}

            {/* ── 3. Supervision Package (optional) ── */}
            {!loadingPkgs && supervisionPkgs.length > 0 && (
              <div>
                <h3 className="text-sm font-semibold text-gray-700 mb-1 flex items-center gap-2">
                  <HardHat size={14} />
                  Supervision Package <span className="text-xs text-gray-400 font-normal">(optional)</span>
                </h3>
                <p className="text-xs text-gray-400 mb-3">Add on-site construction supervision services.</p>
                <PackagePickerGroup
                  packages={supervisionPkgs}
                  selectedId={selectedSupervisionPkgId}
                  onSelect={(id) => setSelectedSupervisionPkgId((prev) => prev === id ? '' : id)}
                  accentColor="orange"
                  allowDeselect
                />
              </div>
            )}

            {/* ── Totals summary ── */}
            {!loadingPkgs && selectedPkgId && (
              <div className="bg-gray-50 border border-gray-200 rounded-lg p-3 text-sm space-y-1">
                {[
                  { label: 'House Design',    pkgs: houseDesignPkgs,  id: selectedPkgId },
                  { label: 'Interior Design', pkgs: interiorPkgs,     id: selectedInteriorPkgId },
                  { label: 'Supervision',     pkgs: supervisionPkgs,  id: selectedSupervisionPkgId },
                ].filter(({ id }) => id).map(({ label, pkgs, id }) => {
                  const pkg = pkgs.find((p) => (p.id ?? p.packageId) === id);
                  if (!pkg) return null;
                  const total = pkg.lineItems.reduce((s, li) => s + li.amountDDFC, 0);
                  return (
                    <div key={label} className="flex justify-between">
                      <span className="text-gray-500">{label} ({pkg.packageTier})</span>
                      <span className="font-medium">PKR {total.toLocaleString()}</span>
                    </div>
                  );
                })}
                <div className="flex justify-between font-bold border-t border-gray-200 pt-1 mt-1">
                  <span>Total</span>
                  <span>PKR {[
                    houseDesignPkgs.find((p) => (p.id ?? p.packageId) === selectedPkgId),
                    interiorPkgs.find((p) => (p.id ?? p.packageId) === selectedInteriorPkgId),
                    supervisionPkgs.find((p) => (p.id ?? p.packageId) === selectedSupervisionPkgId),
                  ].filter(Boolean).reduce((s, p) => s + p!.lineItems.reduce((t, li) => t + li.amountDDFC, 0), 0).toLocaleString()}</span>
                </div>
              </div>
            )}

            {!loadingPkgs && (
              <Button
                variant="primary"
                className="w-full"
                disabled={!selectedPkgId || submitting}
                onClick={handleSelectPackage}
                icon={<Printer size={16} />}
              >
                {submitting ? 'Saving…' : 'Select Package & Move to Payment'}
              </Button>
            )}
          </div>
        )}

        {/* ── Payment Step (after package is selected — challan printed, awaiting bank payment) ── */}
        {isAwaitingPayment && (
          <div className="border border-blue-200 bg-blue-50 rounded-lg p-4 space-y-4">
            <h3 className="text-sm font-semibold text-blue-800 flex items-center gap-2">
              <CreditCard size={14} />
              Step: Payment Confirmation
            </h3>
            <p className="text-xs text-blue-700">
              The package challan has been printed. Once the customer pays at the bank, upload the stamped challan and confirm payment below.
            </p>

            {/* Package summary */}
            {request.packageTier && (
              <div className="space-y-1">
                <div className="flex justify-between items-center bg-white border border-blue-200 rounded px-3 py-2 text-sm">
                  <span className="text-gray-600">
                    <Package size={13} className="inline mr-1" />
                    House Design – {request.packageTier}
                  </span>
                </div>
                {request.interiorDesignPackageTier && (
                  <div className="flex justify-between items-center bg-white border border-blue-200 rounded px-3 py-2 text-sm">
                    <span className="text-gray-600">
                      <Layers size={13} className="inline mr-1" />
                      Interior Design – {request.interiorDesignPackageTier}
                    </span>
                    {request.interiorDesignPackageTotal != null && (
                      <span className="font-medium text-gray-700">PKR {request.interiorDesignPackageTotal.toLocaleString()}</span>
                    )}
                  </div>
                )}
                {request.supervisionPackageTier && (
                  <div className="flex justify-between items-center bg-white border border-blue-200 rounded px-3 py-2 text-sm">
                    <span className="text-gray-600">
                      <HardHat size={13} className="inline mr-1" />
                      Supervision – {request.supervisionPackageTier}
                    </span>
                    {request.supervisionPackageTotal != null && (
                      <span className="font-medium text-gray-700">PKR {request.supervisionPackageTotal.toLocaleString()}</span>
                    )}
                  </div>
                )}
                {request.packageTotal != null && (
                  <div className="flex justify-between items-center bg-blue-100 border border-blue-300 rounded px-3 py-2 text-sm font-bold">
                    <span>Total Amount</span>
                    <span>PKR {request.packageTotal.toLocaleString()}</span>
                  </div>
                )}
              </div>
            )}

            {/* Bank details */}
            <div className="bg-yellow-50 border border-yellow-300 rounded px-3 py-2 text-xs space-y-1">
              <p className="font-semibold text-yellow-800 flex items-center gap-1">
                <Banknote size={12} /> Bank Payment Instructions
              </p>
              <p><span className="text-gray-500">Bank:</span> <strong>Bank Alfalah (Islamic)</strong></p>
              <p><span className="text-gray-500">Account:</span> <strong>MicroChip Enterprises (Pvt) Ltd.</strong></p>
              <p><span className="text-gray-500">IBAN:</span> <strong className="tracking-wider">PK40ALFH56540050023666769</strong></p>
              {request.challanNo && (
                <p><span className="text-gray-500">Challan No:</span> <strong>{request.challanNo}</strong></p>
              )}
            </div>

            {/* Reprint challan */}
            <Button
              variant="secondary"
              className="w-full"
              icon={<Printer size={15} />}
              onClick={handlePrintChallan}
            >
              Reprint Challan
            </Button>

            <hr className="border-blue-200" />

            {isPaymentPendingApproval ? (
              <>
                {/* Challan already submitted — awaiting Possession Admin approval */}
                <div className="bg-amber-50 border border-amber-300 rounded px-3 py-2 text-xs space-y-1">
                  <p className="font-semibold text-amber-800">Challan submitted — awaiting approval</p>
                  <p><span className="text-gray-500">Challan No:</span> <strong>{challanNo || request.challanNo}</strong></p>
                </div>

                {canApprovePayment ? (
                  <Button
                    variant="primary"
                    className="w-full"
                    loading={approving}
                    onClick={handleApprovePayment}
                    icon={<CheckCircle size={16} />}
                  >
                    Approve Payment
                  </Button>
                ) : (
                  <p className="text-xs text-gray-500 text-center py-1">
                    Waiting for Possession Admin to approve this payment.
                  </p>
                )}
              </>
            ) : (
              <>
                {/* Upload paid challan */}
                <p className="text-xs text-gray-500">
                  Once the customer pays at the bank and submits the stamped challan, enter the details below:
                </p>

                <div className="space-y-2">
                  <div>
                    <label className="block text-xs font-medium text-gray-600 mb-1">
                      Challan Number <span className="text-red-500">*</span>
                    </label>
                    <Input
                      value={challanNo}
                      onChange={(e) => setChallanNo(e.target.value)}
                      placeholder={request.challanNo ?? 'e.g. CHN-2026-A1B2C3'}
                    />
                  </div>
                  <FileUploadButton
                    label="Upload Paid Challan (Scanned Copy)"
                    required
                    value={scannedFileUrl}
                    onUploaded={(url) => setScannedFileUrl(url)}
                  />
                </div>

                <Button
                  variant="primary"
                  className="w-full"
                  disabled={paymentSubmitting || !challanNo || !scannedFileUrl}
                  onClick={handleConfirmPayment}
                  icon={<CheckCircle size={16} />}
                >
                  {paymentSubmitting ? 'Submitting…' : 'Submit Challan for Approval'}
                </Button>
              </>
            )}
          </div>
        )}


        {/* ── Payment done + further stages ── */}
        {isPaymentDone && !isFinalApproved && (
          <div className="bg-green-50 border border-green-200 rounded-lg p-4 space-y-2">
            <h3 className="text-sm font-semibold text-green-800 flex items-center gap-2">
              <CheckCircle size={14} />
              Package &amp; Payment Confirmed
            </h3>
            <p className="text-sm text-green-700">
              <strong>{request.packageTier}</strong> package selected.
              {request.packageTotal != null && (
                <> Payment of <strong>PKR {request.packageTotal.toLocaleString()}</strong> recorded.</>
              )}
            </p>
          </div>
        )}

        {/* ── Step 17: Document Delivery ── */}
        {isFinalApproved && (
          <div className="border border-emerald-200 bg-emerald-50 rounded-lg p-4 space-y-4">
            <h3 className="text-sm font-semibold text-emerald-800 flex items-center gap-2">
              <CheckCircle size={14} />
              Final Approval Granted — Ready for Document Delivery
            </h3>
            <p className="text-xs text-emerald-700">
              Print the possession certificate and all design documents, collect them in an envelope,
              and hand over to the customer. Then mark the request as delivered.
            </p>
            <div className="flex flex-col gap-2">
              <Button
                variant="secondary"
                className="w-full"
                icon={<Printer size={15} />}
                onClick={handlePrintCertificate}
              >
                Print Possession Certificate
              </Button>
              <Button
                variant="primary"
                className="w-full"
                loading={delivering}
                icon={<CheckCircle size={15} />}
                onClick={handleDeliver}
              >
                Mark as Delivered &amp; Hand Over Documents
              </Button>
            </div>
          </div>
        )}
      </div>
    </Card>
  );
};

