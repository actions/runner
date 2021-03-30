import React from 'react';
import ReactDOM from 'react-dom';
import { MemoryRouter, Link } from 'react-router-dom';
import { mount, shallow } from 'enzyme';
import { stubMatchMedia } from 'utils-test';
import { Header, HeaderProps, headerEmptyTitle } from './Header';

describe('<Header />', () => {

    let div: any;
    let originalMatchMedia: any;
    let props: HeaderProps;

    beforeEach(() => {
        div = document.createElement('div');
        props = {
            title: 'Test Title',
            hideBackButton: false
        };
        originalMatchMedia = window.matchMedia;
        window.matchMedia = stubMatchMedia(true); 
    });

    afterEach(() => {
        window.matchMedia = originalMatchMedia;
    });

    it('[SMOKE - DEEP] renders without crashing', () => {
        ReactDOM.render(
            <MemoryRouter>
                <Header {...props} />
            </MemoryRouter>, div);
        ReactDOM.unmountComponentAtNode(div);
    });

    it('[SMOKE - SHALLOW] renders without crashing', () => {
        shallow(
            <MemoryRouter>
                <Header {...props} />
            </MemoryRouter>);
    });

    describe('when the component is activated', () => {

        const mountComponent = () => mount(
            <MemoryRouter>
                <Header {...props} />
            </MemoryRouter>);

        beforeEach(() => {
            props = {
                title: 'Test Title',
                hideBackButton: false
            }; 
        });

        it('displays the correct title', () => {
            const component = mountComponent();
            const title = component.find(`[data-test='HeaderTitle']`);
            expect(title.length).toBe(1);
            expect(title.text()).toBe(props.title);
        });

        it('displays an appropriate message if no title is supplied', () => {
            props.title = '';
            const component = mountComponent();
            const title = component.find(`[data-test='HeaderTitle']`);
            expect(title.length).toBe(1);
            expect(title.text()).toBe(headerEmptyTitle);
        });

        describe('the back button', () => {

            it('is a <Link /> that goes BACK two levels up', () => {
                const component = mountComponent();
                const back = component.find(Link);
                expect(back.props().to).toEqual('../../');
            });
    
            it('The back button is visible on smaller screens', () => {
                const component = mountComponent();
                const back = component.find(Link);
                expect(back.length).toBe(1);
            });

            it('The back button is hidden on larger screens (by default)', () => {
                window.matchMedia = stubMatchMedia(false); 
                const component = mountComponent();
                const back = component.find(Link);
                expect(back.length).toBe(0); 
            });

            it('is hidden on smaller screens by the optional prop `hideBackButton`', () => {
                props.hideBackButton = true;
                const component = mountComponent();
                const back = component.find(Link);
                expect(back.get(0).props.style.visibility).toBe('hidden');
            });

        });

    });

});