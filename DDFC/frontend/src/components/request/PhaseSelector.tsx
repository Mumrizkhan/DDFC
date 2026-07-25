import React from 'react';

interface PhaseSelectorProps {
  phases: string[];
  loading: boolean;
  selectedPhase: string;
  onSelect: (phase: string) => void;
  error?: string;
}

export const PhaseSelector: React.FC<PhaseSelectorProps> = ({
  phases,
  loading,
  selectedPhase,
  onSelect,
  error,
}) => (
  <div>
    <label className="block text-sm font-medium text-gray-700 mb-2">
      Phase <span className="text-red-500">*</span>
    </label>
    {loading ? (
      <p className="text-sm text-gray-500">Loading phases...</p>
    ) : (
      <div className="flex flex-wrap gap-2">
        {phases.map((p) => (
          <label
            key={p}
            className={`cursor-pointer select-none px-4 py-2 rounded-full border text-sm font-medium transition-colors ${
              selectedPhase === p
                ? 'bg-blue-600 text-white border-blue-600'
                : 'bg-white text-gray-700 border-gray-300 hover:border-blue-400 hover:text-blue-600'
            }`}
          >
            <input
              type="radio"
              className="sr-only"
              value={p}
              checked={selectedPhase === p}
              onChange={() => onSelect(p)}
            />
            Phase {p}
          </label>
        ))}
      </div>
    )}
    {error && <p className="text-red-500 text-sm mt-1">{error}</p>}
  </div>
);
