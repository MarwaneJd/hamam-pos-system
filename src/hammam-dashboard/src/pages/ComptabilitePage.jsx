import { useState, useEffect } from 'react';
import {
  Calculator,
  Building2,
  Calendar,
  TrendingUp,
  TrendingDown,
  Check,
  X,
  ChevronDown,
  ChevronUp,
  Save,
  Eye,
  Receipt,
  Download,
  FileSpreadsheet
} from 'lucide-react';
import api from '../services/api';
import * as XLSX from 'xlsx';

export default function ComptabilitePage() {
  const [hammams, setHammams] = useState([]);
  const [selectedHammam, setSelectedHammam] = useState('');

  const getTodayDate = () => {
    const now = new Date();
    return `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}-${String(now.getDate()).padStart(2, '0')}`;
  };

  const [dateDebut, setDateDebut] = useState(getTodayDate());
  const [dateFin, setDateFin] = useState(getTodayDate());
  const [resumeData, setResumeData] = useState(null);
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [inlineEdit, setInlineEdit] = useState(null);
  const [expandedJour, setExpandedJour] = useState(null);
  const [jourDetail, setJourDetail] = useState(null);
  const [showDetailModal, setShowDetailModal] = useState(false);
  const [activePeriod, setActivePeriod] = useState('today');
  const [showExportMenu, setShowExportMenu] = useState(false);

  useEffect(() => { fetchHammams(); }, []);
  useEffect(() => { if (selectedHammam) fetchResume(); }, [selectedHammam, dateDebut, dateFin]);

  const fetchHammams = async () => {
    try {
      const response = await api.get('/hammams');
      setHammams(response.data);
      if (response.data.length > 0) setSelectedHammam(response.data[0].id);
    } catch (error) {
      console.error('Erreur chargement hammams:', error);
    }
  };

  const fetchResume = async () => {
    if (!selectedHammam) return;
    setLoading(true);
    try {
      const response = await api.get('/comptabilite/resume', {
        params: { hammamId: selectedHammam, dateDebut, dateFin }
      });
      setResumeData(response.data);
    } catch (error) {
      console.error('Erreur chargement résumé:', error);
    }
    setLoading(false);
  };

  const startInlineEdit = (jour, index) => {
    setInlineEdit({
      date: jour.date,
      value: jour.montantRemis?.toString() || '',
      commentaire: jour.commentaire || '',
      montantTheorique: jour.montantTheorique,
      index
    });
  };

  const saveInlineEdit = async (goToNext = false) => {
    if (!inlineEdit || !inlineEdit.value) return;
    setSaving(true);
    try {
      await api.post('/comptabilite/versement', {
        hammamId: selectedHammam,
        date: inlineEdit.date,
        montantRemis: parseFloat(inlineEdit.value),
        commentaire: inlineEdit.commentaire || null
      });
      const currentIndex = inlineEdit.index;
      setInlineEdit(null);
      await fetchResume();

      if (goToNext && resumeData?.jours && currentIndex + 1 < resumeData.jours.length) {
        const nextJour = resumeData.jours[currentIndex + 1];
        setTimeout(() => startInlineEdit(nextJour, currentIndex + 1), 100);
      }
    } catch (error) {
      console.error('Erreur sauvegarde:', error);
      alert('Erreur lors de la sauvegarde');
    }
    setSaving(false);
  };

  const viewJourDetail = async (date) => {
    try {
      const response = await api.get('/comptabilite/jour-detail', {
        params: { hammamId: selectedHammam, date }
      });
      setJourDetail(response.data);
      setShowDetailModal(true);
    } catch (error) {
      console.error('Erreur:', error);
    }
  };

  const setQuickPeriod = (period) => {
    setActivePeriod(period);
    const now = new Date();
    const fmt = (d) => `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;

    switch (period) {
      case 'today':
        setDateDebut(fmt(now)); setDateFin(fmt(now)); return;
      case 'yesterday': {
        const d = new Date(now); d.setDate(d.getDate() - 1);
        setDateDebut(fmt(d)); setDateFin(fmt(d)); return;
      }
      case 'week': {
        const d = new Date(now); d.setDate(d.getDate() - 7);
        setDateDebut(fmt(d)); setDateFin(fmt(now)); return;
      }
      case 'month': {
        const d = new Date(now.getFullYear(), now.getMonth(), 1);
        setDateDebut(fmt(d)); setDateFin(fmt(now)); return;
      }
    }
  };

  const formatDate = (dateStr) => new Date(dateStr).toLocaleDateString('fr-FR', { weekday: 'short', day: 'numeric', month: 'short' });
  const formatMoney = (amount) => `${amount?.toFixed(2) || '0.00'} DH`;

  const exportToCSV = () => {
    if (!resumeData?.jours?.length) { alert('Aucune donnée à exporter'); return; }
    const headers = ['Date', 'Tickets', 'Théorique (DH)', 'Remis (DH)', 'Écart (DH)', 'Status'];
    const rows = resumeData.jours.map(j => [
      j.date, j.nombreTickets, j.montantTheorique?.toFixed(2) || '0.00',
      j.montantRemis !== null && j.montantRemis !== undefined ? j.montantRemis.toFixed(2) : 'Non saisi',
      j.ecart !== null && j.ecart !== undefined ? j.ecart.toFixed(2) : 'N/A',
      j.estValide ? (j.ecart >= 0 ? 'OK' : 'Déficit') : 'En attente'
    ]);
    rows.push(['TOTAL', resumeData.totalTickets, resumeData.totalTheorique?.toFixed(2), resumeData.totalRemis?.toFixed(2), resumeData.totalEcart?.toFixed(2), '']);
    const csv = [headers, ...rows].map(r => r.map(c => `"${c}"`).join(',')).join('\n');
    const blob = new Blob([`\uFEFF${csv}`], { type: 'text/csv;charset=utf-8;' });
    const link = document.createElement('a');
    const hammamName = hammams.find(h => h.id === selectedHammam)?.nom || 'hammam';
    link.href = URL.createObjectURL(blob);
    link.download = `comptabilite_${hammamName.replace(/\s+/g, '_')}_${dateDebut}_${dateFin}.csv`;
    link.style.visibility = 'hidden';
    document.body.appendChild(link); link.click(); document.body.removeChild(link);
  };

  const exportToExcel = () => {
    if (!resumeData?.jours?.length) { alert('Aucune donnée à exporter'); return; }
    const data = resumeData.jours.map(j => ({
      'Date': j.date, 'Tickets': j.nombreTickets, 'Théorique (DH)': j.montantTheorique || 0,
      'Remis (DH)': j.montantRemis ?? 'Non saisi', 'Écart (DH)': j.ecart ?? 'N/A',
      'Status': j.estValide ? (j.ecart >= 0 ? 'OK' : 'Déficit') : 'En attente'
    }));
    data.push({ 'Date': 'TOTAL', 'Tickets': resumeData.totalTickets, 'Théorique (DH)': resumeData.totalTheorique, 'Remis (DH)': resumeData.totalRemis, 'Écart (DH)': resumeData.totalEcart, 'Status': '' });
    const wb = XLSX.utils.book_new();
    const ws = XLSX.utils.json_to_sheet(data);
    ws['!cols'] = [{ wch: 20 }, { wch: 10 }, { wch: 15 }, { wch: 15 }, { wch: 15 }, { wch: 12 }];
    XLSX.utils.book_append_sheet(wb, ws, 'Comptabilité');
    const hammamName = hammams.find(h => h.id === selectedHammam)?.nom || 'hammam';
    const wbout = XLSX.write(wb, { bookType: 'xlsx', type: 'array' });
    const blob = new Blob([wbout], { type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' });
    const link = document.createElement('a');
    link.href = URL.createObjectURL(blob);
    link.download = `comptabilite_${hammamName.replace(/\s+/g, '_')}_${dateDebut}_${dateFin}.xlsx`;
    document.body.appendChild(link); link.click(); document.body.removeChild(link);
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold text-white flex items-center gap-3">
            <Calculator className="w-8 h-8 text-emerald-500" />
            Comptabilité Journalière
          </h1>
          <p className="text-slate-400 mt-1">Suivi des recettes et versements par jour</p>
        </div>
        {/* Export */}
        <div className="relative">
          <button onClick={() => setShowExportMenu(!showExportMenu)} disabled={!resumeData?.jours?.length || loading}
            className="flex items-center gap-2 px-4 py-2.5 bg-emerald-600 hover:bg-emerald-700 disabled:bg-slate-600 disabled:cursor-not-allowed text-white rounded-lg font-medium transition-colors">
            <Download className="w-5 h-5" /> Exporter <ChevronDown className="w-4 h-4" />
          </button>
          {showExportMenu && (
            <div className="absolute right-0 mt-2 w-48 bg-slate-800 border border-slate-700 rounded-lg shadow-xl z-50">
              <button onClick={() => { exportToCSV(); setShowExportMenu(false); }}
                className="w-full flex items-center gap-3 px-4 py-3 text-white hover:bg-slate-700 rounded-t-lg transition-colors">
                <Download className="w-5 h-5 text-green-400" />
                <div className="text-left"><p className="font-medium">CSV</p><p className="text-xs text-slate-400">Format universel</p></div>
              </button>
              <button onClick={() => { exportToExcel(); setShowExportMenu(false); }}
                className="w-full flex items-center gap-3 px-4 py-3 text-white hover:bg-slate-700 rounded-b-lg transition-colors border-t border-slate-700">
                <FileSpreadsheet className="w-5 h-5 text-emerald-400" />
                <div className="text-left"><p className="font-medium">Excel (.xlsx)</p><p className="text-xs text-slate-400">Microsoft Excel</p></div>
              </button>
            </div>
          )}
        </div>
      </div>

      {/* Filtres */}
      <div className="bg-slate-800/50 backdrop-blur-sm rounded-xl p-6 border border-slate-700/50">
        <div className="grid grid-cols-1 md:grid-cols-5 gap-4">
          <div className="md:col-span-2">
            <label className="block text-sm font-medium text-slate-300 mb-2">
              <Building2 className="w-4 h-4 inline mr-2" />Hammam
            </label>
            <select value={selectedHammam} onChange={(e) => setSelectedHammam(e.target.value)}
              className="w-full bg-slate-700 border border-slate-600 rounded-lg px-4 py-2.5 text-white focus:ring-2 focus:ring-emerald-500">
              {hammams.map((h) => <option key={h.id} value={h.id}>{h.nom}</option>)}
            </select>
          </div>
          <div>
            <label className="block text-sm font-medium text-slate-300 mb-2"><Calendar className="w-4 h-4 inline mr-2" />Du</label>
            <input type="date" value={dateDebut} onChange={(e) => setDateDebut(e.target.value)}
              className="w-full bg-slate-700 border border-slate-600 rounded-lg px-4 py-2.5 text-white focus:ring-2 focus:ring-emerald-500" />
          </div>
          <div>
            <label className="block text-sm font-medium text-slate-300 mb-2"><Calendar className="w-4 h-4 inline mr-2" />Au</label>
            <input type="date" value={dateFin} onChange={(e) => setDateFin(e.target.value)}
              className="w-full bg-slate-700 border border-slate-600 rounded-lg px-4 py-2.5 text-white focus:ring-2 focus:ring-emerald-500" />
          </div>
          <div className="flex flex-col justify-end gap-2">
            <div className="flex gap-2">
              <button onClick={() => setQuickPeriod('today')}
                className={`flex-1 px-3 py-1.5 ${activePeriod === 'today' ? 'bg-emerald-600' : 'bg-slate-600 hover:bg-slate-500'} text-white rounded-lg text-sm`}>
                Aujourd'hui
              </button>
              <button onClick={() => setQuickPeriod('yesterday')}
                className={`flex-1 px-3 py-1.5 ${activePeriod === 'yesterday' ? 'bg-emerald-600' : 'bg-slate-600 hover:bg-slate-500'} text-white rounded-lg text-sm`}>
                Hier
              </button>
            </div>
            <div className="flex gap-2">
              <button onClick={() => setQuickPeriod('week')}
                className={`flex-1 px-3 py-1.5 ${activePeriod === 'week' ? 'bg-emerald-600' : 'bg-slate-600 hover:bg-slate-500'} text-white rounded-lg text-sm`}>
                7 jours
              </button>
              <button onClick={() => setQuickPeriod('month')}
                className={`flex-1 px-3 py-1.5 ${activePeriod === 'month' ? 'bg-emerald-600' : 'bg-slate-600 hover:bg-slate-500'} text-white rounded-lg text-sm`}>
                Ce mois
              </button>
            </div>
          </div>
        </div>
      </div>

      {/* Totaux */}
      {resumeData && resumeData.jours?.length > 0 && (
        <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
          <div className="bg-slate-800/50 rounded-xl p-4 border border-slate-700/50 text-center">
            <p className="text-3xl font-bold text-white">{resumeData.totalTickets}</p>
            <p className="text-sm text-slate-400 mt-1">Total Tickets</p>
          </div>
          <div className="bg-slate-800/50 rounded-xl p-4 border border-slate-700/50 text-center">
            <p className="text-3xl font-bold text-blue-400">{formatMoney(resumeData.totalTheorique)}</p>
            <p className="text-sm text-slate-400 mt-1">Théorique</p>
          </div>
          <div className="bg-slate-800/50 rounded-xl p-4 border border-slate-700/50 text-center">
            <p className="text-3xl font-bold text-yellow-400">{formatMoney(resumeData.totalRemis)}</p>
            <p className="text-sm text-slate-400 mt-1">Total Remis</p>
          </div>
          <div className="bg-slate-800/50 rounded-xl p-4 border border-slate-700/50 text-center">
            <p className={`text-3xl font-bold ${resumeData.totalEcart >= 0 ? 'text-emerald-400' : 'text-red-400'}`}>
              {resumeData.totalEcart >= 0 ? '+' : ''}{formatMoney(resumeData.totalEcart)}
            </p>
            <p className="text-sm text-slate-400 mt-1">Écart Total</p>
          </div>
        </div>
      )}

      {/* Tableau par jour */}
      {loading ? (
        <div className="flex justify-center py-12">
          <div className="animate-spin rounded-full h-12 w-12 border-t-2 border-b-2 border-emerald-500"></div>
        </div>
      ) : resumeData?.jours?.length > 0 ? (
        <div className="bg-slate-800/50 backdrop-blur-sm rounded-xl border border-slate-700/50 overflow-hidden">
          <table className="w-full">
            <thead className="bg-slate-700/50">
              <tr>
                <th className="px-5 py-3 text-left text-sm font-medium text-slate-300">Date</th>
                <th className="px-5 py-3 text-center text-sm font-medium text-slate-300">Tickets</th>
                <th className="px-5 py-3 text-right text-sm font-medium text-slate-300">Théorique</th>
                <th className="px-5 py-3 text-right text-sm font-medium text-slate-300">Remis</th>
                <th className="px-5 py-3 text-right text-sm font-medium text-slate-300">Écart</th>
                <th className="px-5 py-3 text-center text-sm font-medium text-slate-300">Status</th>
                <th className="px-5 py-3 text-center text-sm font-medium text-slate-300">Actions</th>
              </tr>
            </thead>
            <tbody>
              {resumeData.jours.map((jour, idx) => (
                <tr key={idx} className="border-t border-slate-700/30 hover:bg-slate-700/20 transition-colors">
                  {/* Date */}
                  <td className="px-5 py-4">
                    <div className="flex items-center gap-3">
                      <div className="w-10 h-10 bg-slate-700 rounded-lg flex items-center justify-center">
                        <Calendar className="w-5 h-5 text-emerald-400" />
                      </div>
                      <div>
                        <p className="text-white font-medium">{formatDate(jour.date)}</p>
                        {jour.detailParType?.length > 0 && (
                          <p className="text-xs text-slate-500 mt-0.5">
                            {jour.detailParType.map(t => `${t.nom}: ${t.nombre}`).join(' · ')}
                          </p>
                        )}
                      </div>
                    </div>
                  </td>

                  {/* Tickets */}
                  <td className="px-5 py-4 text-center">
                    <span className="text-xl font-bold text-white">{jour.nombreTickets}</span>
                  </td>

                  {/* Théorique */}
                  <td className="px-5 py-4 text-right">
                    <span className="text-lg font-semibold text-blue-400">{formatMoney(jour.montantTheorique)}</span>
                  </td>

                  {/* Remis (éditable) */}
                  <td className="px-5 py-4">
                    {inlineEdit && inlineEdit.date === jour.date ? (
                      <div className="flex items-center gap-2 justify-end">
                        <input type="number" step="0.01" value={inlineEdit.value}
                          onChange={(e) => setInlineEdit({ ...inlineEdit, value: e.target.value })}
                          className="w-28 bg-slate-700 border border-emerald-500 rounded px-2 py-1.5 text-white text-right font-medium focus:ring-2 focus:ring-emerald-500"
                          placeholder="0.00" autoFocus
                          onKeyDown={(e) => { if (e.key === 'Enter') saveInlineEdit(true); if (e.key === 'Escape') setInlineEdit(null); }} />
                        <button onClick={() => saveInlineEdit()} disabled={!inlineEdit.value || saving}
                          className="p-1.5 bg-emerald-500 text-white rounded hover:bg-emerald-600 disabled:opacity-50">
                          <Check className="w-4 h-4" />
                        </button>
                        <button onClick={() => setInlineEdit(null)} className="p-1.5 bg-slate-600 text-white rounded hover:bg-slate-500">
                          <X className="w-4 h-4" />
                        </button>
                      </div>
                    ) : (
                      <div className="text-right cursor-pointer hover:bg-slate-600/50 rounded px-2 py-1 -mr-2"
                        onClick={() => startInlineEdit(jour, idx)}>
                        {jour.montantRemis !== null && jour.montantRemis !== undefined ? (
                          <span className="text-lg font-semibold text-yellow-400">{formatMoney(jour.montantRemis)}</span>
                        ) : (
                          <span className="text-emerald-400 underline italic">Saisir...</span>
                        )}
                      </div>
                    )}
                  </td>

                  {/* Écart */}
                  <td className="px-5 py-4 text-right">
                    {jour.ecart !== null && jour.ecart !== undefined ? (
                      <span className={`text-lg font-bold ${jour.ecart >= 0 ? 'text-emerald-400' : 'text-red-400'}`}>
                        {jour.ecart >= 0 ? '+' : ''}{formatMoney(jour.ecart)}
                      </span>
                    ) : (
                      <span className="text-slate-500">-</span>
                    )}
                  </td>

                  {/* Status */}
                  <td className="px-5 py-4 text-center">
                    {jour.estValide ? (
                      <span className={`inline-flex items-center gap-1 px-2.5 py-1 rounded-full text-xs font-medium ${
                        jour.ecart >= 0 ? 'bg-emerald-500/20 text-emerald-400' : 'bg-red-500/20 text-red-400'}`}>
                        {jour.ecart >= 0 ? <Check className="w-3 h-3" /> : <X className="w-3 h-3" />}
                        {jour.ecart >= 0 ? 'OK' : 'Déficit'}
                      </span>
                    ) : (
                      <span className="inline-flex items-center px-2.5 py-1 rounded-full text-xs font-medium bg-orange-500/20 text-orange-400">
                        En attente
                      </span>
                    )}
                  </td>

                  {/* Actions */}
                  <td className="px-5 py-4 text-center">
                    <button onClick={() => viewJourDetail(jour.date)}
                      className="p-2 bg-blue-500/20 text-blue-400 rounded-lg hover:bg-blue-500/30" title="Voir détails">
                      <Eye className="w-4 h-4" />
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>

            {/* Footer totals */}
            <tfoot className="bg-slate-700/30 border-t-2 border-slate-600">
              <tr>
                <td className="px-5 py-3 text-white font-bold">TOTAL</td>
                <td className="px-5 py-3 text-center text-xl font-bold text-white">{resumeData.totalTickets}</td>
                <td className="px-5 py-3 text-right text-lg font-bold text-blue-400">{formatMoney(resumeData.totalTheorique)}</td>
                <td className="px-5 py-3 text-right text-lg font-bold text-yellow-400">{formatMoney(resumeData.totalRemis)}</td>
                <td className="px-5 py-3 text-right">
                  <span className={`text-lg font-bold ${resumeData.totalEcart >= 0 ? 'text-emerald-400' : 'text-red-400'}`}>
                    {resumeData.totalEcart >= 0 ? '+' : ''}{formatMoney(resumeData.totalEcart)}
                  </span>
                </td>
                <td className="px-5 py-3"></td>
                <td className="px-5 py-3"></td>
              </tr>
            </tfoot>
          </table>
        </div>
      ) : (
        <div className="bg-slate-800/50 rounded-xl p-12 text-center border border-slate-700/50">
          <Calculator className="w-16 h-16 text-slate-600 mx-auto mb-4" />
          <p className="text-slate-400">Aucune donnée pour cette période</p>
          <p className="text-slate-500 text-sm mt-2">Sélectionnez un hammam et une période</p>
        </div>
      )}

      {/* Modal détail jour */}
      {showDetailModal && jourDetail && (
        <div className="fixed inset-0 bg-black/70 flex items-center justify-center z-50">
          <div className="bg-slate-800 rounded-xl p-6 w-full max-w-2xl border border-slate-700 max-h-[80vh] overflow-y-auto">
            <div className="flex justify-between items-center mb-4">
              <h3 className="text-xl font-bold text-white">
                Détail du {formatDate(jourDetail.date)}
              </h3>
              <button onClick={() => setShowDetailModal(false)} className="p-1 hover:bg-slate-700 rounded">
                <X className="w-5 h-5 text-slate-400" />
              </button>
            </div>

            <div className="bg-slate-700/50 rounded-lg p-4 mb-4">
              <p className="text-slate-400">Hammam</p>
              <p className="text-white font-medium">{jourDetail.hammamNom}</p>
            </div>

            <h4 className="text-lg font-semibold text-white mb-3 flex items-center gap-2">
              <Receipt className="w-5 h-5" />
              Tickets vendus ({jourDetail.nombreTickets})
            </h4>

            <div className="bg-slate-700/30 rounded-lg overflow-hidden mb-4">
              <table className="w-full">
                <thead className="bg-slate-700/50">
                  <tr>
                    <th className="px-4 py-2 text-left text-sm text-slate-300">Heure</th>
                    <th className="px-4 py-2 text-left text-sm text-slate-300">Type</th>
                    <th className="px-4 py-2 text-left text-sm text-slate-300">Employé</th>
                    <th className="px-4 py-2 text-right text-sm text-slate-300">Prix</th>
                  </tr>
                </thead>
                <tbody>
                  {jourDetail.tickets.map((ticket, i) => (
                    <tr key={i} className="border-t border-slate-700/30">
                      <td className="px-4 py-2 text-slate-300">
                        {new Date(ticket.heure).toLocaleTimeString('fr-FR', { hour: '2-digit', minute: '2-digit' })}
                      </td>
                      <td className="px-4 py-2 text-white font-medium">{ticket.typeTicket}</td>
                      <td className="px-4 py-2 text-slate-300">{ticket.employe}</td>
                      <td className="px-4 py-2 text-right text-emerald-400">{formatMoney(ticket.prix)}</td>
                    </tr>
                  ))}
                </tbody>
                <tfoot className="bg-slate-700/50">
                  <tr>
                    <td colSpan={3} className="px-4 py-2 text-right font-bold text-white">Total</td>
                    <td className="px-4 py-2 text-right font-bold text-blue-400">{formatMoney(jourDetail.montantTheorique)}</td>
                  </tr>
                </tfoot>
              </table>
            </div>

            {jourDetail.estValide && (
              <div className={`rounded-lg p-4 ${jourDetail.ecart >= 0 ? 'bg-emerald-500/20' : 'bg-red-500/20'}`}>
                <div className="flex justify-between items-center">
                  <div>
                    <p className={jourDetail.ecart >= 0 ? 'text-emerald-300' : 'text-red-300'}>Versement enregistré</p>
                    <p className="text-white">Montant remis: <span className="font-bold">{formatMoney(jourDetail.montantRemis)}</span></p>
                  </div>
                  <div className="text-right">
                    <p className={jourDetail.ecart >= 0 ? 'text-emerald-300' : 'text-red-300'}>Écart</p>
                    <p className={`text-2xl font-bold ${jourDetail.ecart >= 0 ? 'text-emerald-400' : 'text-red-400'}`}>
                      {jourDetail.ecart >= 0 ? '+' : ''}{formatMoney(jourDetail.ecart)}
                    </p>
                  </div>
                </div>
                {jourDetail.commentaire && (
                  <p className="text-slate-300 mt-2 text-sm">💬 {jourDetail.commentaire}</p>
                )}
              </div>
            )}
          </div>
        </div>
      )}
    </div>
  );
}
