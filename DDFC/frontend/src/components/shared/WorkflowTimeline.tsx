import React from 'react';
import { format } from 'date-fns';
import type { WorkflowHistoryEntry } from '../../types';
import { Badge } from '../ui/Badge';

interface WorkflowTimelineProps {
  history: WorkflowHistoryEntry[];
  showActor?: boolean;
}

export const WorkflowTimeline: React.FC<WorkflowTimelineProps> = ({
  history,
  showActor = true,
}) => (
  <div className="flow-root">
    <ul className="-mb-8">
      {history.map((entry, idx) => (
        <li key={entry.historyId}>
          <div className="relative pb-8">
            {idx < history.length - 1 && (
              <span
                className="absolute left-4 top-4 -ml-px h-full w-0.5 bg-gray-200"
                aria-hidden
              />
            )}
            <div className="relative flex space-x-3">
              <div>
                <span className="h-8 w-8 rounded-full bg-blue-500 flex items-center justify-center ring-8 ring-white">
                  <span className="text-white text-xs font-bold">
                    {(entry.toStatus || 'S').charAt(0)}
                  </span>
                </span>
              </div>
              <div className="flex min-w-0 flex-1 justify-between space-x-4 pt-1.5">
                <div>
                  <p className="text-sm text-gray-800">
                    Status changed to{' '}
                    <Badge variant="info">{entry.toStatus}</Badge>
                  </p>
                  {entry.comments && (
                    <p className="mt-1 text-sm text-gray-500 italic">
                      "{entry.comments}"
                    </p>
                  )}
                  {showActor && entry.actorName && (
                    <p className="mt-0.5 text-xs text-gray-400">
                      by {entry.actorName}
                    </p>
                  )}
                </div>
                <div className="whitespace-nowrap text-right text-xs text-gray-400">
                  {format(new Date(entry.timestamp), 'dd MMM yyyy HH:mm')}
                </div>
              </div>
            </div>
          </div>
        </li>
      ))}
    </ul>
  </div>
);
