#pragma once

#include "models.h"
#include "services.h"
#include <QHash>
#include <QPoint>
#include <QSystemTrayIcon>
#include <QTimer>
#include <QWidget>
#include <optional>

class QLabel;
class QScrollArea;
class QVBoxLayout;
class ModelRowWidget;

class WidgetWindow final : public QWidget {
    Q_OBJECT
public:
    WidgetWindow(Settings &settings, SettingsStore &store, UsagePoller &poller);
protected:
    void mousePressEvent(QMouseEvent *event) override;
    void mouseMoveEvent(QMouseEvent *event) override;
    void mouseReleaseEvent(QMouseEvent *event) override;
    void contextMenuEvent(QContextMenuEvent *event) override;
    void closeEvent(QCloseEvent *event) override;
    void resizeEvent(QResizeEvent *event) override;
    void keyPressEvent(QKeyEvent *event) override;
private:
    enum ResizeAnchorFlag { SpansX = 1, SpansY = 2, TopLeftQuadrant = 4, TopRightQuadrant = 8, BottomRightQuadrant = 16, BottomLeftQuadrant = 32 };
    void applySnapshot(const DayUsageSnapshot &snapshot);
    void showError(const QString &error);
    void toggleExpanded(); void saveSoon(); void clampToScreen(); void center(bool horizontal);
    void buildMenu(const QPoint &globalPosition); void updateRows(const QList<ModelBreakdown> &models);
    QString modelKey(const ModelBreakdown &model) const;
    static int computeResizeAnchorFlags(const QRect &geometry, const QPoint &screenCenter);
    Settings &m_settings; SettingsStore &m_store; UsagePoller &m_poller;
    QLabel *m_total; QLabel *m_empty; QLabel *m_error; QScrollArea *m_scroll; QWidget *m_rowsHost; QVBoxLayout *m_cardLayout; QVBoxLayout *m_rowsLayout;
    QHash<QString, ModelRowWidget *> m_rows; QHash<QString, QString> m_previousCosts;
    QTimer m_highlightTimer; QTimer m_saveTimer; QPoint m_dragStart; bool m_dragging = false; bool m_firstUpdate = true; bool m_exitRequested = false;
    std::optional<int> m_previousResizeAnchor;
};
