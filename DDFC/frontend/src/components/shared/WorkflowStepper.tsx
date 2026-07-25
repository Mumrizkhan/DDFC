import React, { useState } from 'react';
import { CheckCircle, Clock, Circle, ChevronDown, ChevronUp } from 'lucide-react';
import type { RequestStatus } from '../../types';

const WORKFLOW_STEPS: { status: RequestStatus; label: string }[] = [
  { status: 'Submitted', label: 'Submitted' },
  { status: 'Initiated', label: 'Initiated' },
  { status: 'TransferApproved', label: 'Transfer' },
  { status: 'FinanceApproved', label: 'Finance' },
  { status: 'BothBranchesCleared', label: 'Building Control' },
  { status: 'PossessionLetterSigned', label: 'Admin Signed' },
  { status: 'PackageSelected', label: 'Package' },
  { status: 'PackagePaid', label: 'Payment' },
  { status: 'ArchitectAssigned', label: 'PA Review' },
  { status: 'ArchitectureApproved', label: 'Architecture' },
  { status: 'ThreeDCompleted', label: '3D' },
  { status: 'StructureCompleted', label: 'Structure' },
  { status: 'MEPCompleted', label: 'MEP' },
  { status: 'PrincipalArchitectApproved', label: 'Principal Arch.' },
  { status: 'BuildingControlCompleted', label: 'Building Control' },
  { status: 'FinalApproved', label: 'Final Approval' },
  { status: 'Delivered', label: 'Delivered' },
];

/** Maps WorkflowEngine step names to the stepper status they represent. */
const WE_STEP_TO_STATUS: Record<string, RequestStatus> = {
  'Reception – Submit NOC/NDC Request':          'Initiated',
  'Transfer Branch – NOC/NDC Review':            'TransferApproved',
  'Finance Branch – Dues Clearance':             'FinanceApproved',
  'DDFC Admin – Sign Possession Letter':         'PossessionLetterSigned',
  'Reception – Package Selection':               'PackageSelected',
  'Finance Branch – Payment Confirmation':       'PackagePaid',
  'Principal Architect – Initial Review':         'ArchitectAssigned',
  'Architecture Department – House Plan Design': 'ArchitectureApproved',
  'Architecture Department – 3D Visualization':  'ThreeDCompleted',
  'Structure Department – Structural Design':    'StructureCompleted',
  'MEP Department – MEP Design':                 'MEPCompleted',
  'Principal Architect – Design Review':         'PrincipalArchitectApproved',
  'Building Control – Physical Survey':          'BuildingControlCompleted',
  'DHA Design Head – Final Approval':            'FinalApproved',
  'Reception – Document Delivery':               'Delivered',
};

const STATUS_ORDER = WORKFLOW_STEPS.map((s) => s.status);
const TOTAL = WORKFLOW_STEPS.length;

interface WorkflowStepperProps {
  currentStatus: RequestStatus;
  activeStepNames?: string[];
}

export const WorkflowStepper: React.FC<WorkflowStepperProps> = ({
  currentStatus,
  activeStepNames,
}) => {
  const [expanded, setExpanded] = useState(false);

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
  }, [activeStepNames]);

  const hasWeData = weActiveIndices.size > 0;
  const fallbackIdx = STATUS_ORDER.indexOf(currentStatus);
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
