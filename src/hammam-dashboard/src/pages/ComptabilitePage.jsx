import { useState, useEffect } from 'react';
import {
  Calculator,
  Building2,
  Calendar,
  User,
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

  // Fonction pour obtenir la date du jour au format YYYY-MM-DD
  const getTodayDate = () => {
    const now = new Date();
    const year = now.getFullYear();
    const month = String(now.getMonth() + 1).padStart(2, '0');
    const day = String(now.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
  };

  const [dateDebut, setDateDebut] = useState(getTodayDate());
  const [dateFin, setDateFin] = useState(getTodayDate());
  const [resumeData, setResumeData] = useState(null);
  const [loading, setLoading] = useState(false);
  const [expandedEmploye, setExpandedEmploye] = useState(null);
  const [editingVersement, setEditingVersement] = useState(null);
  const [montantRemis, setMontantRemis] = useState('');
  const [commentaire, setCommentaire] = useState('');
  const [saving, setSaving] = useState(false);
  const [jourDetail, setJourDetail] = useState(null);
  const [showDetailModal, setShowDetailModal] = useState(false);
  const [inlineEdit, setInlineEdit] = useState(null); // {employeId, date, value}
  const [activePeriod, setActivePeriod] = useState('today');

  useEffect(() => {
    fetchHammams();
  }, []);

  useEffect(() => {
    if (selectedHammam) {
      fetchResume();
    }
  }, [selectedHammam, dateDebut, dateFin]);

  const fetchHammams = async () => {
    try {
      const response = await api.get('/hammams');
      setHammams(response.data);
      if (response.data.length > 0) {
        setSelectedHammam(response.data[0].id);
      }
    } catch (error) {
      console.error('Erreur chargement hammams:', error);
    }
  };

  const fetchResume = async () => {
    if (!selectedHammam) return;

    setLoading(true);
    try {
      const response = await api.get('/comptabilite/resume', {
        params: {
          hammamId: selectedHammam,
          dateDebut,
          dateFin
        }
      });
      setResumeData(response.data);
    } catch (error) {
      console.error('Erreur chargement résumé:', error);
    }
    setLoading(false);
  };

  const openEditVersement = (employe, jour) => {
    setEditingVersement({ employe, jour });
    setMontantRemis(jour.montantRemis?.toString() || '');
    setCommentaire(jour.commentaire || '');
  };

  const saveVersement = async () => {
    if (!editingVersement || !montantRemis) return;

    setSaving(true);
    try {
      await api.post('/comptabilite/versement', {
        employeId: editingVersement.employe.employeId,
        date: editingVersement.jour.date,
        montantRemis: parseFloat(montantRemis),
        commentaire: commentaire || null
      });

      setEditingVersement(null);
      setMontantRemis('');
      setCommentaire('');
      fetchResume();
    } catch (error) {
      console.error('Erreur sauvegarde:', error);
      alert('Erreur lors de la sauvegarde');
    }
    setSaving(false);
  };

  const viewJourDetail = async (employeId, date) => {
    try {
      const response = await api.get('/comptabilite/jour-detail', {
        params: { employeId, date }
      });
      setJourDetail(response.data);
      setShowDetailModal(true);
    } catch (error) {
      console.error('Erreur:', error);
    }
  };

  const startInlineEdit = (employeId, jour, jourIndex = 0, employeIndex = 0) => {
    setInlineEdit({
      employeId,
      date: jour.date,
      value: jour.montantRemis?.toString() || '',
      montantTheorique: jour.montantTheorique,
      jourIndex,
      employeIndex
    });
  };

  const saveInlineEdit = async (goToNext = false) => {
    if (!inlineEdit || !inlineEdit.value) return;

    setSaving(true);
    try {
      await api.post('/comptabilite/versement', {
        employeId: inlineEdit.employeId,
        date: inlineEdit.date,
        montantRemis: parseFloat(inlineEdit.value),
        commentaire: null
      });

      const currentJourIndex = inlineEdit.jourIndex;
      const currentEmployeIndex = inlineEdit.employeIndex;
      
      setInlineEdit(null);
      await fetchResume();

      // Si on doit aller à la ligne suivante
      if (goToNext && resumeData?.employes) {
        const currentEmploye = resumeData.employes[currentEmployeIndex];
        if (currentEmploye) {
          // Vérifier s'il y a une ligne suivante dans le même employé
          if (currentJourIndex + 1 < currentEmploye.joursDetails.length) {
            const nextJour = currentEmploye.joursDetails[currentJourIndex + 1];
            setTimeout(() => {
              startInlineEdit(currentEmploye.employeId, nextJour, currentJourIndex + 1, currentEmployeIndex);
            }, 100);
          } else if (currentEmployeIndex + 1 < resumeData.employes.length) {
            // Sinon, passer au premier jour de l'employé suivant
            const nextEmploye = resumeData.employes[currentEmployeIndex + 1];
            if (nextEmploye.joursDetails?.length > 0) {
              // Ouvrir d'abord l'employé suivant
              setExpandedEmploye(nextEmploye.employeId);
              setTimeout(() => {
                startInlineEdit(nextEmploye.employeId, nextEmploye.joursDetails[0], 0, currentEmployeIndex + 1);
              }, 100);
            }
          }
        }
      }
    } catch (error) {
      console.error('Erreur sauvegarde:', error);
      alert('Erreur lors de la sauvegarde');
    }
    setSaving(false);
  };

  const setQuickPeriod = (period) => {
    setActivePeriod(period);
    const now = new Date();
    const formatDate = (d) => {
      const year = d.getFullYear();
      const month = String(d.getMonth() + 1).padStart(2, '0');
      const day = String(d.getDate()).padStart(2, '0');
      return `${year}-${month}-${day}`;
    };

    let debut = new Date();

    switch (period) {
      case 'today':
        setDateDebut(formatDate(now));
        setDateFin(formatDate(now));
        return;
      case 'yesterday':
        debut = new Date(now);
        debut.setDate(debut.getDate() - 1);
        setDateDebut(formatDate(debut));
        setDateFin(formatDate(debut));
        return;
      case 'week':
        debut = new Date(now);
        debut.setDate(debut.getDate() - 7);
        break;
      case 'month':
        debut = new Date(now.getFullYear(), now.getMonth(), 1);
        break;
    }

    setDateDebut(formatDate(debut));
    setDateFin(formatDate(now));
  };

  const formatDate = (dateStr) => {
    return new Date(dateStr).toLocaleDateString('fr-FR', {
      weekday: 'short',
      day: 'numeric',
      month: 'short'
    });
  };

  const formatMoney = (amount) => {
    return `${amount?.toFixed(2) || '0.00'} DH`;
  };

  // Export CSV function
  const exportToCSV = () => {
    if (!resumeData?.employes?.length) {
      alert('Aucune donnée à exporter');
      return;
    }

    // CSV Headers
    const headers = [
      'Employé',
      'Username',
      'Date',
      'Tickets',
      'Théorique (DH)',
      'Remis (DH)',
      'Écart (DH)',
      'Status'
    ];

    // CSV Rows
    const rows = [];

    resumeData.employes.forEach(employe => {
      employe.joursDetails.forEach(jour => {
        rows.push([
          employe.employeNom,
          employe.username,
          jour.date,
          jour.nombreTickets,
          jour.montantTheorique?.toFixed(2) || '0.00',
          jour.montantRemis?.toFixed(2) || 'Non saisi',
          jour.ecart !== null ? jour.ecart.toFixed(2) : 'N/A',
          jour.estValide ? (jour.ecart >= 0 ? 'OK' : 'Déficit') : 'En attente'
        ]);
      });

      // Add summary row for employee
      rows.push([
        `TOTAL - ${employe.employeNom}`,
        '',
        `${dateDebut} - ${dateFin}`,
        employe.totalTickets,
        employe.totalTheorique?.toFixed(2) || '0.00',
        employe.totalRemis?.toFixed(2) || '0.00',
        employe.totalEcart?.toFixed(2) || '0.00',
        '---'
      ]);
      rows.push([]); // Empty row separator
    });

    // Build CSV content
    const csvContent = [headers, ...rows]
      .map(row => row.map(cell => `"${cell}"`).join(','))
      .join('\n');

    // Create and download file
    const blob = new Blob([`\uFEFF${csvContent}`], { type: 'text/csv;charset=utf-8;' });
    const link = document.createElement('a');
    const url = URL.createObjectURL(blob);

    const hammamName = hammams.find(h => h.id === selectedHammam)?.nom || 'hammam';
    const filename = `comptabilite_${hammamName.replace(/\s+/g, '_')}_${dateDebut}_${dateFin}.csv`;

    link.setAttribute('href', url);
    link.setAttribute('download', filename);
    link.style.visibility = 'hidden';
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
  };

  // Export Excel function
  const exportToExcel = () => {
    if (!resumeData?.employes?.length) {
      alert('Aucune donnée à exporter');
      return;
    }

    // Prepare data for Excel
    const allData = [];

    resumeData.employes.forEach(employe => {
      employe.joursDetails.forEach(jour => {
        allData.push({
          'Employé': employe.employeNom,
          'Username': employe.username,
          'Date': jour.date,
          'Tickets': jour.nombreTickets,
          'Théorique (DH)': jour.montantTheorique || 0,
          'Remis (DH)': jour.montantRemis !== null ? jour.montantRemis : 'Non saisi',
          'Écart (DH)': jour.ecart !== null ? jour.ecart : 'N/A',
          'Status': jour.estValide ? (jour.ecart >= 0 ? 'OK' : 'Déficit') : 'En attente'
        });
      });

      // Add summary row
      allData.push({
        'Employé': `TOTAL - ${employe.employeNom}`,
        'Username': '',
        'Date': `${dateDebut} - ${dateFin}`,
        'Tickets': employe.totalTickets,
        'Théorique (DH)': employe.totalTheorique || 0,
        'Remis (DH)': employe.totalRemis || 0,
        'Écart (DH)': employe.totalEcart || 0,
        'Status': '---'
      });
      allData.push({}); // Empty row separator
    });

    // Create workbook and worksheet
    const wb = XLSX.utils.book_new();
    const ws = XLSX.utils.json_to_sheet(allData);

    // Set column widths
    ws['!cols'] = [
      { wch: 25 }, // Employé
      { wch: 15 }, // Username
      { wch: 25 }, // Date
      { wch: 10 }, // Tickets
      { wch: 15 }, // Théorique
      { wch: 15 }, // Remis
      { wch: 15 }, // Écart
      { wch: 12 }  // Status
    ];

    XLSX.utils.book_append_sheet(wb, ws, 'Comptabilité');

    // Generate filename and download
    const hammamName = hammams.find(h => h.id === selectedHammam)?.nom || 'hammam';
    const filename = `comptabilite_${hammamName.replace(/\s+/g, '_')}_${dateDebut}_${dateFin}.xlsx`;

    // Write file with proper options
    const wbout = XLSX.write(wb, { bookType: 'xlsx', type: 'array' });
    const blob = new Blob([wbout], { type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = filename;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(url);
  };

  const [showExportMenu, setShowExportMenu] = useState(false);

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold text-white flex items-center gap-3">
            <Calculator className="w-8 h-8 text-emerald-500" />
            Comptabilité Employés
          </h1>
          <p className="text-slate-400 mt-1">
            Gestion des versements et suivi des écarts
          </p>
        </div>

        {/* Export Dropdown */}
        <div className="relative">
          <button
            onClick={() => setShowExportMenu(!showExportMenu)}
            disabled={!resumeData?.employes?.length || loading}
            className="flex items-center gap-2 px-4 py-2.5 bg-emerald-600 hover:bg-emerald-700 disabled:bg-slate-600 disabled:cursor-not-allowed text-white rounded-lg font-medium transition-colors"
          >
            <Download className="w-5 h-5" />
            Exporter
            <ChevronDown className="w-4 h-4" />
          </button>

          {showExportMenu && (
            <div className="absolute right-0 mt-2 w-48 bg-slate-800 border border-slate-700 rounded-lg shadow-xl z-50">
              <button
                onClick={() => { exportToCSV(); setShowExportMenu(false); }}
                className="w-full flex items-center gap-3 px-4 py-3 text-white hover:bg-slate-700 rounded-t-lg transition-colors"
              >
                <Download className="w-5 h-5 text-green-400" />
                <div className="text-left">
                  <p className="font-medium">CSV</p>
                  <p className="text-xs text-slate-400">Format universel</p>
                </div>
              </button>
              <button
                onClick={() => { exportToExcel(); setShowExportMenu(false); }}
                className="w-full flex items-center gap-3 px-4 py-3 text-white hover:bg-slate-700 rounded-b-lg transition-colors border-t border-slate-700"
              >
                <FileSpreadsheet className="w-5 h-5 text-emerald-400" />
                <div className="text-left">
                  <p className="font-medium">Excel (.xlsx)</p>
                  <p className="text-xs text-slate-400">Microsoft Excel</p>
                </div>
              </button>
            </div>
          )}
        </div>
      </div>

      {/* Filtres */}
      <div className="bg-slate-800/50 backdrop-blur-sm rounded-xl p-6 border border-slate-700/50">
        <div className="grid grid-cols-1 md:grid-cols-5 gap-4">
          {/* Sélection Hammam */}
          <div className="md:col-span-2">
            <label className="block text-sm font-medium text-slate-300 mb-2">
              <Building2 className="w-4 h-4 inline mr-2" />
              Hammam
            </label>
            <select
              value={selectedHammam}
              onChange={(e) => setSelectedHammam(e.target.value)}
              className="w-full bg-slate-700 border border-slate-600 rounded-lg px-4 py-2.5 text-white focus:ring-2 focus:ring-emerald-500"
            >
              {hammams.map((h) => (
                <option key={h.id} value={h.id}>{h.nom}</option>
              ))}
            </select>
          </div>

          {/* Date début */}
          <div>
            <label className="block text-sm font-medium text-slate-300 mb-2">
              <Calendar className="w-4 h-4 inline mr-2" />
              Du
            </label>
            <input
              type="date"
              value={dateDebut}
              onChange={(e) => setDateDebut(e.target.value)}
              className="w-full bg-slate-700 border border-slate-600 rounded-lg px-4 py-2.5 text-white focus:ring-2 focus:ring-emerald-500"
            />
          </div>

          {/* Date fin */}
          <div>
            <label className="block text-sm font-medium text-slate-300 mb-2">
              <Calendar className="w-4 h-4 inline mr-2" />
              Au
            </label>
            <input
              type="date"
              value={dateFin}
              onChange={(e) => setDateFin(e.target.value)}
              className="w-full bg-slate-700 border border-slate-600 rounded-lg px-4 py-2.5 text-white focus:ring-2 focus:ring-emerald-500"
            />
          </div>

          {/* Boutons rapides */}
          <div className="flex flex-col justify-end gap-2">
            <div className="flex gap-2">
              <button
                onClick={() => setQuickPeriod('today')}
                className={`flex-1 px-3 py-1.5 ${activePeriod === 'today' ? 'bg-emerald-600 hover:bg-emerald-700' : 'bg-slate-600 hover:bg-slate-500'} text-white rounded-lg text-sm`}
              >
                Aujourd'hui
              </button>
              <button
                onClick={() => setQuickPeriod('yesterday')}
                className={`flex-1 px-3 py-1.5 ${activePeriod === 'yesterday' ? 'bg-emerald-600 hover:bg-emerald-700' : 'bg-slate-600 hover:bg-slate-500'} text-white rounded-lg text-sm`}
              >
                Hier
              </button>
            </div>
            <div className="flex gap-2">
              <button
                onClick={() => setQuickPeriod('week')}
                className={`flex-1 px-3 py-1.5 ${activePeriod === 'week' ? 'bg-emerald-600 hover:bg-emerald-700' : 'bg-slate-600 hover:bg-slate-500'} text-white rounded-lg text-sm`}
              >
                7 jours
              </button>
              <button
                onClick={() => setQuickPeriod('month')}
                className={`flex-1 px-3 py-1.5 ${activePeriod === 'month' ? 'bg-emerald-600 hover:bg-emerald-700' : 'bg-slate-600 hover:bg-slate-500'} text-white rounded-lg text-sm`}
              >
                Ce mois
              </button>
            </div>
          </div>
        </div>
      </div>

      {/* Liste des employés */}
      {loading ? (
        <div className="flex justify-center py-12">
          <div className="animate-spin rounded-full h-12 w-12 border-t-2 border-b-2 border-emerald-500"></div>
        </div>
      ) : resumeData?.employes?.length > 0 ? (
        <div className="space-y-4">
          {resumeData.employes.map((employe, employeIndex) => (
            <div key={employe.employeId} className="bg-slate-800/50 backdrop-blur-sm rounded-xl border border-slate-700/50 overflow-hidden">
              {/* Header employé */}
              <div
                className="p-4 cursor-pointer hover:bg-slate-700/30 transition-colors"
                onClick={() => setExpandedEmploye(expandedEmploye === employe.employeId ? null : employe.employeId)}
              >
                <div className="flex items-center justify-between">
                  <div className="flex items-center gap-4">
                    <div className="w-12 h-12 bg-gradient-to-br from-emerald-500 to-teal-600 rounded-full flex items-center justify-center">
                      <User className="w-6 h-6 text-white" />
                    </div>
                    <div>
                      <h3 className="text-lg font-semibold text-white">{employe.employeNom}</h3>
                      <p className="text-sm text-slate-400">@{employe.username}</p>
                    </div>
                  </div>

                  <div className="flex items-center gap-6">
                    {/* Stats */}
                    <div className="text-center">
                      <p className="text-2xl font-bold text-white">{employe.totalTickets}</p>
                      <p className="text-xs text-slate-400">Tickets</p>
                    </div>
                    <div className="text-center">
                      <p className="text-2xl font-bold text-blue-400">{formatMoney(employe.totalTheorique)}</p>
                      <p className="text-xs text-slate-400">Théorique</p>
                    </div>
                    <div className="text-center">
                      <p className="text-2xl font-bold text-yellow-400">{formatMoney(employe.totalRemis)}</p>
                      <p className="text-xs text-slate-400">Remis</p>
                    </div>
                    <div className="text-center">
                      <p className={`text-2xl font-bold ${employe.totalEcart >= 0 ? 'text-emerald-400' : 'text-red-400'}`}>
                        {employe.totalEcart >= 0 ? '+' : ''}{formatMoney(employe.totalEcart)}
                      </p>
                      <p className="text-xs text-slate-400">Écart</p>
                    </div>

                    {/* Icône écart */}
                    <div className={`w-10 h-10 rounded-full flex items-center justify-center ${employe.totalEcart >= 0 ? 'bg-emerald-500/20' : 'bg-red-500/20'
                      }`}>
                      {employe.totalEcart >= 0 ? (
                        <TrendingUp className="w-5 h-5 text-emerald-400" />
                      ) : (
                        <TrendingDown className="w-5 h-5 text-red-400" />
                      )}
                    </div>

                    {/* Expand */}
                    {expandedEmploye === employe.employeId ? (
                      <ChevronUp className="w-5 h-5 text-slate-400" />
                    ) : (
                      <ChevronDown className="w-5 h-5 text-slate-400" />
                    )}
                  </div>
                </div>
              </div>

              {/* Détails par jour */}
              {expandedEmploye === employe.employeId && (
                <div className="border-t border-slate-700/50">
                  <table className="w-full">
                    <thead className="bg-slate-700/30">
                      <tr>
                        <th className="px-4 py-3 text-left text-sm font-medium text-slate-300">Date</th>
                        <th className="px-4 py-3 text-center text-sm font-medium text-slate-300">Tickets</th>
                        <th className="px-4 py-3 text-right text-sm font-medium text-slate-300">Théorique</th>
                        <th className="px-4 py-3 text-right text-sm font-medium text-slate-300">Remis</th>
                        <th className="px-4 py-3 text-right text-sm font-medium text-slate-300">Écart</th>
                        <th className="px-4 py-3 text-center text-sm font-medium text-slate-300">Status</th>
                        <th className="px-4 py-3 text-center text-sm font-medium text-slate-300">Actions</th>
                      </tr>
                    </thead>
                    <tbody>
                      {employe.joursDetails.map((jour, jourIndex) => (
                        <tr key={jourIndex} className="border-t border-slate-700/30 hover:bg-slate-700/20">
                          <td className="px-4 py-3 text-white font-medium">
                            {formatDate(jour.date)}
                          </td>
                          <td className="px-4 py-3 text-center text-slate-300">
                            {jour.nombreTickets}
                          </td>
                          <td className="px-4 py-3 text-right text-blue-400 font-medium">
                            {formatMoney(jour.montantTheorique)}
                          </td>
                          <td className="px-4 py-3">
                            {inlineEdit && inlineEdit.employeId === employe.employeId && inlineEdit.date === jour.date ? (
                              <div className="flex items-center gap-2 justify-end">
                                <input
                                  type="number"
                                  step="0.01"
                                  value={inlineEdit.value}
                                  onChange={(e) => setInlineEdit({ ...inlineEdit, value: e.target.value })}
                                  className="w-24 bg-slate-700 border border-emerald-500 rounded px-2 py-1 text-white text-right font-medium focus:ring-2 focus:ring-emerald-500"
                                  placeholder="0.00"
                                  autoFocus
                                  onKeyDown={(e) => {
                                    if (e.key === 'Enter') saveInlineEdit(true);
                                    if (e.key === 'Escape') setInlineEdit(null);
                                  }}
                                />
                                <button
                                  onClick={saveInlineEdit}
                                  disabled={!inlineEdit.value || saving}
                                  className="p-1 bg-emerald-500 text-white rounded hover:bg-emerald-600 disabled:opacity-50"
                                >
                                  <Check className="w-4 h-4" />
                                </button>
                                <button
                                  onClick={() => setInlineEdit(null)}
                                  className="p-1 bg-slate-600 text-white rounded hover:bg-slate-500"
                                >
                                  <X className="w-4 h-4" />
                                </button>
                              </div>
                            ) : (
                              <div
                                className="text-right cursor-pointer hover:bg-slate-600/50 rounded px-2 py-1 -mr-2"
                                onClick={() => startInlineEdit(employe.employeId, jour, jourIndex, employeIndex)}
                              >
                                {jour.montantRemis !== null ? (
                                  <span className="text-yellow-400 font-medium">
                                    {formatMoney(jour.montantRemis)}
                                  </span>
                                ) : (
                                  <span className="text-emerald-400 underline italic">Saisir...</span>
                                )}
                              </div>
                            )}
                          </td>
                          <td className="px-4 py-3 text-right">
                            {jour.ecart !== null ? (
                              <span className={`font-bold ${jour.ecart >= 0 ? 'text-emerald-400' : 'text-red-400'}`}>
                                {jour.ecart >= 0 ? '+' : ''}{formatMoney(jour.ecart)}
                              </span>
                            ) : (
                              <span className="text-slate-500">-</span>
                            )}
                          </td>
                          <td className="px-4 py-3 text-center">
                            {jour.estValide ? (
                              <span className={`inline-flex items-center gap-1 px-2 py-1 rounded-full text-xs font-medium ${jour.ecart >= 0
                                  ? 'bg-emerald-500/20 text-emerald-400'
                                  : 'bg-red-500/20 text-red-400'
                                }`}>
                                {jour.ecart >= 0 ? <Check className="w-3 h-3" /> : <X className="w-3 h-3" />}
                                {jour.ecart >= 0 ? 'OK' : 'Déficit'}
                              </span>
                            ) : (
                              <span className="inline-flex items-center px-2 py-1 rounded-full text-xs font-medium bg-orange-500/20 text-orange-400">
                                En attente
                              </span>
                            )}
                          </td>
                          <td className="px-4 py-3 text-center">
                            <div className="flex justify-center gap-2">
                              <button
                                onClick={() => viewJourDetail(employe.employeId, jour.date)}
                                className="p-1.5 bg-blue-500/20 text-blue-400 rounded hover:bg-blue-500/30"
                                title="Voir détails"
                              >
                                <Eye className="w-4 h-4" />
                              </button>
                              <button
                                onClick={() => openEditVersement(employe, jour)}
                                className="p-1.5 bg-emerald-500/20 text-emerald-400 rounded hover:bg-emerald-500/30"
                                title="Saisir versement"
                              >
                                <Save className="w-4 h-4" />
                              </button>
                            </div>
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              )}
            </div>
          ))}
        </div>
      ) : (
        <div className="bg-slate-800/50 rounded-xl p-12 text-center border border-slate-700/50">
          <Calculator className="w-16 h-16 text-slate-600 mx-auto mb-4" />
          <p className="text-slate-400">Aucune donnée pour cette période</p>
          <p className="text-slate-500 text-sm mt-2">Sélectionnez un hammam et une période</p>
        </div>
      )}

      {/* Modal saisie versement */}
      {editingVersement && (
        <div className="fixed inset-0 bg-black/70 flex items-center justify-center z-50 p-4">
          <div className="bg-slate-800 rounded-xl p-6 w-full max-w-md border border-slate-700 max-h-[90vh] overflow-y-auto">
            <h3 className="text-xl font-bold text-white mb-4">
              Saisir le versement
            </h3>

            <div className="space-y-4">
              <div className="bg-slate-700/50 rounded-lg p-4">
                <p className="text-slate-400">Employé</p>
                <p className="text-white font-medium">{editingVersement.employe.employeNom}</p>
              </div>

              <div className="bg-slate-700/50 rounded-lg p-4">
                <p className="text-slate-400">Date</p>
                <p className="text-white font-medium">{formatDate(editingVersement.jour.date)}</p>
              </div>

              <div className="bg-blue-500/20 rounded-lg p-4">
                <p className="text-blue-300">Montant théorique (calculé)</p>
                <p className="text-2xl font-bold text-blue-400">
                  {formatMoney(editingVersement.jour.montantTheorique)}
                </p>
                <p className="text-sm text-blue-300/70">
                  {editingVersement.jour.nombreTickets} tickets vendus
                </p>
              </div>

              <div>
                <label className="block text-sm font-medium text-slate-300 mb-2">
                  Montant remis par l'employé (DH)
                </label>
                <input
                  type="number"
                  step="0.01"
                  value={montantRemis}
                  onChange={(e) => setMontantRemis(e.target.value)}
                  className="w-full bg-slate-700 border border-slate-600 rounded-lg px-4 py-3 text-white text-xl font-bold focus:ring-2 focus:ring-emerald-500"
                  placeholder="0.00"
                  autoFocus
                />
              </div>

              {montantRemis && (
                <div className={`rounded-lg p-4 ${parseFloat(montantRemis) >= editingVersement.jour.montantTheorique
                    ? 'bg-emerald-500/20'
                    : 'bg-red-500/20'
                  }`}>
                  <p className={parseFloat(montantRemis) >= editingVersement.jour.montantTheorique ? 'text-emerald-300' : 'text-red-300'}>
                    Écart
                  </p>
                  <p className={`text-2xl font-bold ${parseFloat(montantRemis) >= editingVersement.jour.montantTheorique
                      ? 'text-emerald-400'
                      : 'text-red-400'
                    }`}>
                    {(parseFloat(montantRemis) - editingVersement.jour.montantTheorique) >= 0 ? '+' : ''}
                    {formatMoney(parseFloat(montantRemis) - editingVersement.jour.montantTheorique)}
                  </p>
                </div>
              )}

              <div>
                <label className="block text-sm font-medium text-slate-300 mb-2">
                  Commentaire (optionnel)
                </label>
                <textarea
                  value={commentaire}
                  onChange={(e) => setCommentaire(e.target.value)}
                  className="w-full bg-slate-700 border border-slate-600 rounded-lg px-4 py-2 text-white focus:ring-2 focus:ring-emerald-500"
                  rows={2}
                  placeholder="Ex: erreur de caisse, ticket annulé..."
                />
              </div>

              <div className="flex gap-3 pt-4">
                <button
                  onClick={() => setEditingVersement(null)}
                  className="flex-1 px-4 py-2 bg-slate-600 hover:bg-slate-500 text-white rounded-lg"
                >
                  Annuler
                </button>
                <button
                  onClick={saveVersement}
                  disabled={!montantRemis || saving}
                  className="flex-1 px-4 py-2 bg-emerald-600 hover:bg-emerald-700 text-white rounded-lg disabled:opacity-50 flex items-center justify-center gap-2"
                >
                  {saving ? (
                    <div className="animate-spin rounded-full h-5 w-5 border-t-2 border-b-2 border-white"></div>
                  ) : (
                    <>
                      <Save className="w-5 h-5" />
                      Enregistrer
                    </>
                  )}
                </button>
              </div>
            </div>
          </div>
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
              <button
                onClick={() => setShowDetailModal(false)}
                className="p-1 hover:bg-slate-700 rounded"
              >
                <X className="w-5 h-5 text-slate-400" />
              </button>
            </div>

            <div className="bg-slate-700/50 rounded-lg p-4 mb-4">
              <div className="flex justify-between">
                <div>
                  <p className="text-slate-400">Employé</p>
                  <p className="text-white font-medium">{jourDetail.employeNom}</p>
                </div>
                <div className="text-right">
                  <p className="text-slate-400">Hammam</p>
                  <p className="text-white font-medium">{jourDetail.hammamNom}</p>
                </div>
              </div>
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
                    <th className="px-4 py-2 text-right text-sm text-slate-300">Prix</th>
                  </tr>
                </thead>
                <tbody>
                  {jourDetail.tickets.map((ticket, idx) => (
                    <tr key={idx} className="border-t border-slate-700/30">
                      <td className="px-4 py-2 text-slate-300">
                        {new Date(ticket.heure).toLocaleTimeString('fr-FR', { hour: '2-digit', minute: '2-digit' })}
                      </td>
                      <td className="px-4 py-2 text-white font-medium">{ticket.typeTicket}</td>
                      <td className="px-4 py-2 text-right text-emerald-400">{formatMoney(ticket.prix)}</td>
                    </tr>
                  ))}
                </tbody>
                <tfoot className="bg-slate-700/50">
                  <tr>
                    <td colSpan={2} className="px-4 py-2 text-right font-bold text-white">Total</td>
                    <td className="px-4 py-2 text-right font-bold text-blue-400">
                      {formatMoney(jourDetail.montantTheorique)}
                    </td>
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
                  <p className="text-slate-300 mt-2 text-sm">
                    💬 {jourDetail.commentaire}
                  </p>
                )}
              </div>
            )}
          </div>
        </div>
      )}
    </div>
  );
}
