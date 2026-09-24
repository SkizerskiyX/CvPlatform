import { Table } from 'react-bootstrap';
import { useTranslation } from 'react-i18next';

export type Column<T> = {
  key: string;
  header: string;
  render: (row: T) => React.ReactNode;
};

type DataTableProps<T> = {
  columns: Column<T>[];
  rows: T[];
  rowKey: (row: T) => string;
  selectedIds: string[];
  onSelectionChange: (ids: string[]) => void;
  toolbar?: React.ReactNode;
  emptyText: string;
  onRowClick?: (row: T) => void;
};

export function DataTable<T>({
  columns,
  rows,
  rowKey,
  selectedIds,
  onSelectionChange,
  toolbar,
  emptyText,
  onRowClick,
}: DataTableProps<T>) {
  const { i18n } = useTranslation();
  const ru = i18n.language.startsWith('ru');
  const allSelected = rows.length > 0 && rows.every((row) => selectedIds.includes(rowKey(row)));

  const toggleAll = () => onSelectionChange(allSelected ? [] : rows.map(rowKey));

  const toggleOne = (id: string) =>
    onSelectionChange(selectedIds.includes(id) ? selectedIds.filter((value) => value !== id) : [...selectedIds, id]);

  return (
    <div className="data-table">
      <div className="data-table-toolbar">{toolbar}</div>
      <Table responsive hover size="sm" className="align-middle">
        <thead>
          <tr>
            <th className="selection-cell">
              <input type="checkbox" checked={allSelected} onChange={toggleAll} aria-label="select-all" />
            </th>
            {columns.map((column) => (
              <th key={column.key}>{column.header}</th>
            ))}
          </tr>
        </thead>
        <tbody>
          {rows.map((row) => {
            const id = rowKey(row);
            return (
              <tr key={id} className={onRowClick ? 'clickable-row' : undefined} data-selected={selectedIds.includes(id)} onClick={(event) => { if (!(event.target as HTMLElement).closest('a,button,input,select,textarea')) onRowClick?.(row); }}>
                <td className="selection-cell" onClick={(event) => event.stopPropagation()}>
                  <input
                    type="checkbox"
                    checked={selectedIds.includes(id)}
                    onChange={() => toggleOne(id)}
                    aria-label={id}
                  />
                </td>
                {columns.map((column) => (
                  <td key={column.key}>{column.render(row)}</td>
                ))}
              </tr>
            );
          })}
          {rows.length === 0 && (
            <tr>
              <td colSpan={columns.length + 1} className="empty-state">
                {emptyText}
              </td>
            </tr>
          )}
        </tbody>
      </Table>
      <div className="table-footer"><span>{rows.length} {ru ? 'записей' : 'records'}</span><span>{selectedIds.length ? (ru ? 'Выбрано: ' : 'Selected: ') + selectedIds.length : (ru ? 'Выберите строки для действий' : 'Select rows to manage them')}</span></div>
    </div>
  );
}
