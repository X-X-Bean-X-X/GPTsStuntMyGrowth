/*!
 * Cities: Skylines II UI Module
 *
 * Id: GPTStuntYourGrowth
 * Author: X_Bean_X
 * Version: 1.75.633
 * Dependencies:
 */
const { createElement } = window.React;
const { Button, Tooltip } = window["cs2/ui"];
const { trigger } = window["cs2/api"];
const title = "GPT's Stunt My Growth";

function GrowthToolbarButton() {
    return createElement(Tooltip, { tooltip: title },
        createElement(Button, {
            variant: "floating",
            src: "coui://ui-mods/GPTStuntGrowthSkyline.svg",
            "aria-label": title,
            tooltipLabel: title,
            onSelect: () => trigger("GPTStuntYourGrowth", "openSettings")
        }));
}

export const hasCSS = false;

export default function register(registry) {
    registry.append("GameTopLeft", GrowthToolbarButton);
}
