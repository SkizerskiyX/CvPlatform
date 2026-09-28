import { Link } from 'react-router-dom';
import type { Cv } from '../api/types';
import { useText } from '../hooks/useText';
export function CvTable({ rows }: { rows: Cv[] }) {
  const text = useText();
  return <div className="table-responsive"><table className="table"><thead><tr><th>{text('Candidate', 'Кандидат')}</th><th>{text('Position', 'Позиция')}</th><th>{text('Status', 'Статус')}</th><th>{text('Likes', 'Лайки')}</th></tr></thead><tbody>{rows.map(x => <tr key={x.id}><td><Link to={'/profiles/' + x.profileId}>{x.candidateName}</Link></td><td><Link to={'/cvs/' + x.id}>{x.positionTitle}</Link></td><td>{x.status === 'Published' ? text('Published', 'Опубликовано') : text('Draft', 'Черновик')}</td><td>{x.likeCount}</td></tr>)}{!rows.length && <tr><td colSpan={4}>{text('Nothing to display', 'Нет данных')}</td></tr>}</tbody></table></div>;
}
