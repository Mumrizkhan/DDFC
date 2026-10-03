import React, { useState } from 'react';
import { CheckCircle, Clock, Circle, ChevronDown, ChevronUp } from 'lucide-react';
import type { RequestStatus, RequestType } from '../../types';

type StepDef = { status: RequestStatus; label: string };

// ── DDFC Possession & House Design Workflow (20 steps) ──────────────────────
const DDFC_STEPS: StepDef[] = [
  { status: 'Submitted',                 label: 'Submitted' },
  { status: 'DocumentsVerification',         label: 'Documents Verification' },
  { status: 'TransferApproved',          label: 'Transfer' },
  { status: 'FinanceApproved',           label: 'Finance' },
  { status: 'AdCoordApproved',           label: 'AD Coordinator' },
  { status: 'BcdLetterUploaded',         label: 'BCD' },
  { status: 'PossessionLetterSigned',    label: 'Admin' },
  { status: 'PackageSelected',           label: 'Package' },
  { status: 'PackagePaid',               label: 'Payment' },
  { status: 'AdminReviewPending',        label: 'Admin Review' },
  { status: 'SoilTestCompleted',         label: 'Soil Test' },
  { status: 'ArchitectAssigned',         label: 'Principal Architect' },
  { status: 'ArchitectureApproved',      label: 'Architect' },
  { status: 'ThreeDCompleted',           label: '3D' },
  { status: 'ThreeDDraftPending',        label: 'Architect Draft' },
  { status: 'StructureCompleted',        label: 'Structure' },
  { status: 'MEPCompleted',              label: 'MEP' },
  { status: 'PrincipalArchitectApproved', label: 'Principal Architect Review' },
  { status: 'BuildingControlCompleted',  label: 'Building Control' },
  { status: 'FinalApproved',             label: 'Final Approval' },
  { status: 'Delivered',                 label: 'Delivered' },
];

// ── Revised Plan / As-Built Plan Workflow (13 steps — no Transfer/Finance/DDFC Admin) ─
const REVISED_STEPS: StepDef[] = [
  { status: 'Submitted',                  label: 'Submitted' },
  { status: 'DocumentsVerification',          label: 'Documents Verification' },
  { status: 'PackageSelected',            label: 'Package' },
  { status: 'PackagePaid',                label: 'Payment' },
  { status: 'ArchitectAssigned',          label: 'Principal Architect' },
  { status: 'ArchitectureApproved',       label: 'Architect' },
  { status: 'ThreeDCompleted',            label: '3D' },
  { status: 'StructureCompleted',         label: 'Structure' },
  { status: 'MEPCompleted',               label: 'MEP' },
  { status: 'PrincipalArchitectApproved', label: 'Principal Architect Review' },
  { status: 'BuildingControlCompleted',   label: 'Building Control' },
  { status: 'FinalApproved',              label: 'Final Approval' },
  { status: 'Delivered',                  label: 'Delivered' },
];

// Maps WorkflowEngine step name → the status that step represents in the stepper.
const DDFC_WE_MAP: Record<string, RequestStatus> = {
  'Reception \u2013 Submit NOC/NDC Request':         'Submitted',
  'Reception \u2013 Documents Verification':        'DocumentsVerification',
  'Admin \u2013 Document Review':                    'DocumentsVerification',
  'Transfer Branch \u2013 NOC/NDC Review':           'TransferApproved',
  'Finance Branch \u2013 Dues Clearance':            'FinanceApproved',
  'AD Coordinator \u2013 Review':                    'AdCoordApproved',
  'BCD \u2013 Upload Possession Letter':             'BcdLetterUploaded',
  'DDFC Admin \u2013 Sign Possession Letter':        'PossessionLetterSigned',
  'Reception \u2013 Package Selection':              'PackageSelected',
  'Reception \u2013 Payment Confirmation':           'PackagePaid',
  'Admin \u2013 Post-Payment Review':                'AdminReviewPending',
  'Soil Test':                                       'SoilTestCompleted',
  'Principal Architect \u2013 Initial Review':       'ArchitectAssigned',
  'Architect Department \u2013 House Plan Design':   'ArchitectureApproved',
  'Architecture Department \u2013 3D Visualization': 'ThreeDCompleted',
  'Architect \u2013 Assign 3D Drafter & Upload Draft': 'ThreeDDraftPending',
  'Structure Department \u2013 Structural Design':   'StructureCompleted',
  'MEP Department \u2013 MEP Design':                'MEPCompleted',
  'Principal Architect \u2013 Design Review':        'PrincipalArchitectApproved',
  'Building Control \u2013 Physical Survey':         'BuildingControlCompleted',
  'DHA Design Head \u2013 Final Approval':           'FinalApproved',
  'Reception \u2013 Document Delivery':              'Delivered',
};

const REVISED_WE_MAP: Record<string, RequestStatus> = {
  'Reception \u2013 Submit NOC/NDC Request':         'Submitted',
  'Admin \u2013 Document Review':                    'DocumentsVerification',
  'Reception \u2013 Package Selection':              'PackageSelected',
  'Reception \u2013 Payment Confirmation':           'PackagePaid',
  'Principal Architect \u2013 Initial Review':       'ArchitectAssigned',
  'Architect Department \u2013 House Plan Design':   'ArchitectureApproved',
  'Architecture Department \u2013 3D Visualization': 'ThreeDCompleted',
  'Structure Department \u2013 Structural Design':   'StructureCompleted',
  'MEP Department \u2013 MEP Design':                'MEPCompleted',
  'Principal Architect \u2013 Design Review':        'PrincipalArchitectApproved',
  'Building Control \u2013 Physical Survey':         'BuildingControlCompleted',
  'DHA Design Head \u2013 Final Approval':           'FinalApproved',
  'Reception \u2013 Document Delivery':              'Delivered',
};

interface WorkflowStepperProps {
  currentStatus: RequestStatus;
  activeStepNames?: string[];
  requestType?: RequestType;
}

export const WorkflowStepper: React.FC<WorkflowStepperProps> = ({
  currentStatus,
  activeStepNames,
  requestType,
}) => {
  const [expanded, setExpanded] = useState(false);

  const isDdfc = !requestType || requestType === 'PossessionDesign';
  const WORKFLOW_STEPS = isDdfc ? DDFC_STEPS : REVISED_STEPS;
  const WE_STEP_TO_STATUS = isDdfc ? DDFC_WE_MAP : REVISED_WE_MAP;
  const STATUS_ORDER = WORKFLOW_STEPS.map((s) => s.status);
  const TOTAL = WORKFLOW_STEPS.length;

  const weActiveIndices: Set<number> = React.useMemo(() => {
    if (!activeStepNames || activeStepNames.length === 0) return new Set();
    const indices = new Set<number>();
    for (const name of activeStepNames) {
      const status = WE_STEP_TO_STATUS[name];
      if (status) {
        const idx = STATUS_ORDER.indexOf(status);
        if (idx >= 0) indices.add(idx);
      }
    }
    return indices;
  }, [activeStepNames, STATUS_ORDER, WE_STEP_TO_STATUS]);

  const hasWeData = weActiveIndices.size > 0;
  // BothBranchesCleared is not a stepper step; map it to AD Coordinator (in progress)
  const resolvedStatus = currentStatus === 'BothBranchesCleared' ? 'AdCoordApproved'
    : currentStatus === 'PrincipalArchitectReviewPending' ? 'PrincipalArchitectApproved' : currentStatus;
  const fallbackIdx = STATUS_ORDER.indexOf(resolvedStatus);
  const minActiveIdx = hasWeData ? Math.min(...weActiveIndices) : fallbackIdx;

  // 1-based step number, clamped
  const currentStepNum = Math.min(Math.max(minActiveIdx + 1, 1), TOTAL);
  const currentLabel = WORKFLOW_STEPS[minActiveIdx]?.label ?? currentStatus;
  const progressPct = TOTAL > 1 ? Math.round((minActiveIdx / (TOTAL - 1)) * 100) : 100;
  const isComplete = currentStatus === 'Delivered';

  return (
    <div className="space-y-2">
      {/* ── Slim summary bar ── */}
      <div className="flex items-center gap-3">
        {/* Progress bar */}
        <div className="flex-1 h-2 bg-gray-100 rounded-full overflow-hidden">
          <div
            className={`h-full rounded-full transition-all duration-500 ${isComplete ? 'bg-green-500' : 'bg-blue-500'}`}
            style={{ width: `${isComplete ? 100 : progressPct}%` }}
          />
        </div>

        {/* Step label */}
        <span className="text-xs text-gray-500 whitespace-nowrap shrink-0">
          {isComplete ? (
            <span className="text-green-600 font-medium">Completed</span>
          ) : (
            <>
              <span className="font-semibold text-gray-700">Step {currentStepNum}</span>
              <span className="text-gray-400"> of {TOTAL}</span>
              <span className="mx-1 text-gray-300">—</span>
              <span className="text-blue-600 font-medium">{currentLabel}</span>
            </>
          )}
        </span>

        {/* Toggle */}
        <button
          onClick={() => setExpanded((v) => !v)}
          className="flex items-center gap-1 text-xs text-gray-400 hover:text-gray-600 transition-colors shrink-0"
        >
          {expanded ? (
            <>Hide <ChevronUp size={13} /></>
          ) : (
            <>All steps <ChevronDown size={13} /></>
          )}
        </button>
      </div>

      {/* ── Expanded step list ── */}
      {expanded && (
        <div className="pt-1 border-t border-gray-100">
          <ol className="grid grid-cols-2 sm:grid-cols-4 gap-x-4 gap-y-1 py-2">
            {WORKFLOW_STEPS.map((step, idx) => {
              const isActive  = hasWeData ? weActiveIndices.has(idx) : idx === fallbackIdx;
              const isDone    = idx < minActiveIdx;
              const isPending = !isDone && !isActive;

              return (
                <li key={step.status} className="flex items-center gap-1.5 min-w-0">
                  {isDone ? (
                    <CheckCircle size={13} className="text-green-500 shrink-0" />
                  ) : isActive ? (
                    <Clock size={13} className="text-blue-500 shrink-0" />
                  ) : (
                    <Circle size={13} className="text-gray-300 shrink-0" />
                  )}
                  <span
                    className={`text-xs truncate
                      ${isDone    ? 'text-green-700' : ''}
                      ${isActive  ? 'text-blue-700 font-semibold' : ''}
                      ${isPending ? 'text-gray-400' : ''}`}
                  >
                    {idx + 1}. {step.label}
                  </span>
                </li>
              );
            })}
          </ol>
        </div>
      )}
    </div>
  );
};
