import { Form } from 'react-bootstrap';
import Markdown from 'react-markdown';
import Select from 'react-select';
import { useTranslation } from 'react-i18next';
import type { AttributeDataType, AttributeOption, AttributeValueInput } from '../api/types';

type AttributeValueEditorProps = {
  dataType: AttributeDataType;
  options: AttributeOption[];
  value: AttributeValueInput;
  onChange: (value: AttributeValueInput) => void;
};

export function AttributeValueEditor({ dataType, options, value, onChange }: AttributeValueEditorProps) {
  const { t } = useTranslation();

  if (dataType === 'Text') {
    return (
      <div>
        <Form.Control
          as="textarea"
          rows={4}
          value={value.stringValue ?? ''}
          onChange={(event) => onChange({ ...value, stringValue: event.target.value })}
        />
        <div className="markdown-preview">
          <Markdown>{value.stringValue ?? ''}</Markdown>
        </div>
      </div>
    );
  }

  if (dataType === 'String') {
    return (
      <Form.Control value={value.stringValue ?? ''} onChange={(event) => onChange({ ...value, stringValue: event.target.value })} />
    );
  }

  if (dataType === 'Numeric') {
    return (
      <Form.Control
        type="number"
        value={value.numericValue ?? ''}
        onChange={(event) => onChange({ ...value, numericValue: event.target.value === '' ? null : Number(event.target.value) })}
      />
    );
  }

  if (dataType === 'Date') {
    return (
      <Form.Control
        type="date"
        value={value.dateValue?.slice(0, 10) ?? ''}
        onChange={(event) => onChange({ ...value, dateValue: event.target.value ? new Date(event.target.value).toISOString() : null })}
      />
    );
  }

  if (dataType === 'Period') {
    return (
      <div className="d-flex gap-2">
        <Form.Control
          type="date"
          value={value.periodStart?.slice(0, 10) ?? ''}
          onChange={(event) => onChange({ ...value, periodStart: event.target.value ? new Date(event.target.value).toISOString() : null })}
        />
        <Form.Control
          type="date"
          value={value.periodEnd?.slice(0, 10) ?? ''}
          onChange={(event) => onChange({ ...value, periodEnd: event.target.value ? new Date(event.target.value).toISOString() : null })}
        />
      </div>
    );
  }

  if (dataType === 'Boolean') {
    return (
      <Form.Check
        type="switch"
        checked={value.boolValue ?? false}
        label={value.boolValue ? t('common.yes') : t('common.no')}
        onChange={(event) => onChange({ ...value, boolValue: event.target.checked })}
      />
    );
  }

  if (dataType === 'Dropdown') {
    const selectOptions = options.map((option) => ({ value: option.id, label: option.value }));
    return (
      <Select
        options={selectOptions}
        value={selectOptions.find((option) => option.value === value.selectedOptionId) ?? null}
        onChange={(option) => onChange({ ...value, selectedOptionId: option?.value ?? null })}
      />
    );
  }

  return <div><Form.Control type="url" placeholder="https://cloud.example/image.jpg" value={value.imageUrl ?? ''} onChange={(event) => onChange({ ...value, imageUrl: event.target.value || null })} />{value.imageUrl && <img className="photo-preview mt-2" src={value.imageUrl} alt={t('profile.photo')} />}</div>;
}
