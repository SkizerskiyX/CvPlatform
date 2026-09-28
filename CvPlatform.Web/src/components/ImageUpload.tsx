import { useEffect, useState } from 'react';
import { useDropzone } from 'react-dropzone';
import { Alert, Form } from 'react-bootstrap';
import { apiClient } from '../api/client';
import { useText } from '../hooks/useText';
type Configuration = { enabled: boolean; cloudName: string; uploadPreset: string };
export function ImageUpload({ value, onChange }: { value: string | null; onChange: (url: string) => void }) {
  const text = useText(); const [config, setConfig] = useState<Configuration | null>(null); const [busy, setBusy] = useState(false); const [error, setError] = useState('');
  useEffect(() => { void apiClient.get<Configuration>('/api/uploads/configuration').then(x => setConfig(x.data)).catch(e => setError(e.message)); }, []);
  const upload = async (files: File[]) => {
    if (!config?.enabled || !files[0] || busy) return;
    setBusy(true); setError('');
    try {
      const body = new FormData(); body.append('file', files[0]); body.append('upload_preset', config.uploadPreset);
      const response = await fetch('https://api.cloudinary.com/v1_1/' + encodeURIComponent(config.cloudName) + '/image/upload', { method: 'POST', body });
      const result = await response.json();
      if (!response.ok || typeof result.secure_url !== 'string' || !result.secure_url.startsWith('https://')) throw new Error(text('Upload failed. Try again.', 'Не удалось загрузить изображение. Повторите попытку.'));
      onChange(result.secure_url);
    } catch (e) { setError((e as Error).message); } finally { setBusy(false); }
  };
  const { getRootProps, getInputProps, isDragActive } = useDropzone({ onDropAccepted: files => void upload(files), onDropRejected: () => setError(text('Choose one JPG, PNG or WebP image up to 5 MB.', 'Выберите одно изображение JPG, PNG или WebP до 5 МБ.')), accept: { 'image/jpeg': ['.jpg', '.jpeg'], 'image/png': ['.png'], 'image/webp': ['.webp'] }, maxSize: 5 * 1024 * 1024, maxFiles: 1, disabled: busy || !config?.enabled });
  return <div className="d-grid gap-2"><div {...getRootProps({ className: 'border rounded p-3 text-center', role: 'button', 'aria-disabled': busy || !config?.enabled })}><input {...getInputProps()} />{busy ? text('Uploading…', 'Загрузка…') : config?.enabled ? isDragActive ? text('Drop image here', 'Отпустите изображение') : text('Drop an image or click to choose', 'Перетащите изображение или нажмите для выбора') : text('Image uploads are not configured yet.', 'Загрузка изображений пока не настроена.')}</div>{error && <Alert variant="danger">{error}</Alert>}<Form.Control type="url" aria-label={text('Image URL', 'Ссылка на изображение')} placeholder="https://" value={value ?? ''} onChange={e => onChange(e.target.value)} />{value && <img src={value} className="photo-preview" alt={text('Profile image', 'Изображение профиля')} />}</div>;
}
